using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.EmailTemplates;
using CorpServe.Services.Specifications;
using CorpServe.Shared.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class PaymentOverdueBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan OverduePeriod = TimeSpan.FromDays(15);
        private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentOverdueBackgroundService> _logger;

        public PaymentOverdueBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PaymentOverdueBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckOverduePaymentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed while checking overdue payments.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task CheckOverduePaymentsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var adminMonitorService = scope.ServiceProvider.GetRequiredService<IAdminMonitorService>();

            var paymentRepo = unitOfWork.GetRepository<Payment, string>();
            var utcNow = DateTime.UtcNow;
            var overdueThreshold = utcNow - OverduePeriod;

            var pendingPayments = (await paymentRepo.GetAllAsync(new OverduePendingPaymentsSpecification(overdueThreshold))).ToList();

            var processed = 0;
            var warnedClientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var suspendedClientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var payment in pendingPayments)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var client = payment.Client;
                if (client is null)
                    continue;

                if (client.PaymentOverdueWarnedAt is null)
                {
                    if (!warnedClientIds.Add(client.Id))
                        continue;

                    await SendOverdueWarningAsync(client, payment, notificationService, emailService, userManager);
                    processed++;
                }
                else if (utcNow - client.PaymentOverdueWarnedAt.Value >= GracePeriod)
                {
                    if (!suspendedClientIds.Add(client.Id))
                        continue;

                    const string reason = "Your account was suspended because a payment has been Pending for more than 16 days.";
                    await adminMonitorService.SuspendUserAsync(client.Id, reason);
                    processed++;
                }
            }

            if (processed > 0)
                _logger.LogInformation("Payment overdue monitor processed {Count} client(s).", processed);
        }

        private async Task SendOverdueWarningAsync(
            ApplicationUser client,
            Payment payment,
            INotificationService notificationService,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager)
        {
            try
            {
                await notificationService.SendNotificationAsync(
                    client.Id,
                    NotificationTitles.PaymentOverdue,
                    $"Payment for request '{payment.Request?.Title ?? payment.RequestId}' is overdue. Please pay within 24 hours to avoid account suspension.",
                    NotificationTypes.Warning,
                    payment.RequestId,
                    "Request",
                    sendEmail: false);

                if (!string.IsNullOrWhiteSpace(client.Email) && (client.UserPreference?.EmailNotification ?? true))
                {
                    var template = CorpServeEmailTemplateFactory.BuildPaymentOverdueWarning(
                        client.FullName ?? "Client",
                        payment.Request?.Title ?? payment.RequestId,
                        payment.TotalAmount);
                    await emailService.SendEmailAsync(client.Email, template.Subject, template.Body);
                }

                client.PaymentOverdueWarnedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(client);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send overdue warning for client {ClientId}, payment {PaymentId}.", client.Id, payment.Id);
            }
        }
    }
}
