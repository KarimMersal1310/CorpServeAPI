using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Specifications;
using CorpServe.Shared.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class SLAStatusMonitorBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan WarningWindow = TimeSpan.FromHours(72);
        private static readonly TimeSpan WarningCooldown = TimeSpan.FromHours(12);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SLAStatusMonitorBackgroundService> _logger;
        private readonly Dictionary<string, DateTime> _lastWarningSentAt = new(StringComparer.OrdinalIgnoreCase);

        public SLAStatusMonitorBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SLAStatusMonitorBackgroundService> logger)
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
                    await MonitorContractsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed while monitoring SLA contracts.");
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

        private async Task MonitorContractsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var slaRepository = unitOfWork.GetRepository<SLAContract, string>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var contracts = (await slaRepository.GetAllAsync(new ActiveSlaContractsForMonitoringSpecification())).ToList();
            if (contracts.Count == 0)
                return;

            var hasChanges = false;
            var utcNow = DateTime.UtcNow;

            foreach (var contract in contracts)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (contract.Request.RequestStatus == RequestStatus.Completed)
                {
                    if (contract.SLAStatus != SLAStatus.Completed)
                    {
                        contract.SLAStatus = SLAStatus.Completed;
                        slaRepository.Update(contract);
                        hasChanges = true;

                        await NotifyInAppAsync(
                            notificationService,
                            [contract.ClientId, contract.VendorId],
                            NotificationTitles.SlaCompleted,
                            $"SLA for request '{contract.Request.Title}' is completed.",
                            NotificationTypes.Success,
                            contract.RequestId,
                            "Request");
                    }

                    continue;
                }

                var isSuspendedContract = contract.Client.Status == UserStatus.Suspended || contract.Vendor.Status == UserStatus.Suspended;
                if (isSuspendedContract)
                {
                    if (contract.SLAStatus == SLAStatus.Inprogress)
                    {
                        contract.SLAStatus = SLAStatus.Delayed;
                        slaRepository.Update(contract);
                        hasChanges = true;
                    }

                    if (ShouldSendWarning(contract.Id, utcNow))
                    {
                        await NotifyInAppAsync(
                            notificationService,
                            [contract.ClientId, contract.VendorId],
                            NotificationTitles.SlaBlocked,
                            $"SLA for request '{contract.Request.Title}' is blocked because one account is suspended.",
                            NotificationTypes.Error,
                            contract.RequestId,
                            "Request");
                    }

                    continue;
                }

                var remaining = contract.Deadline - utcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    if (contract.SLAStatus != SLAStatus.Delayed)
                    {
                        contract.SLAStatus = SLAStatus.Delayed;
                        slaRepository.Update(contract);
                        hasChanges = true;
                    }

                    if (ShouldSendWarning(contract.Id, utcNow))
                    {
                        await NotifyInAppAsync(
                            notificationService,
                            [contract.ClientId, contract.VendorId],
                            NotificationTitles.SlaDelayed,
                            $"SLA for request '{contract.Request.Title}' is delayed because deadline passed.",
                            NotificationTypes.Error,
                            contract.RequestId,
                            "Request");
                    }

                    continue;
                }

                if (contract.SLAStatus == SLAStatus.Inprogress && remaining <= WarningWindow && ShouldSendWarning(contract.Id, utcNow))
                {
                    await NotifyInAppAsync(
                        notificationService,
                        [contract.ClientId, contract.VendorId],
                        NotificationTitles.SlaDeadlineWarning,
                        $"SLA for request '{contract.Request.Title}' is close to deadline.",
                        NotificationTypes.Warning,
                        contract.RequestId,
                        "Request");
                }
            }

            if (hasChanges)
                await unitOfWork.SaveChangesAsync();

            _logger.LogInformation("SLA monitor checked {ContractsCount} contract(s).", contracts.Count);
        }

        private async Task NotifyInAppAsync(
            INotificationService notificationService,
            IEnumerable<string> recipients,
            string title,
            string message,
            string type,
            string? relatedEntityId,
            string? relatedEntityType)
        {
            var result = await notificationService.SendNotificationToManyAsync(recipients, title, message, type, relatedEntityId, relatedEntityType, sendEmail: false);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to create in-app SLA notifications. Errors: {Errors}",
                    string.Join(" | ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }
        }

        private bool ShouldSendWarning(string contractId, DateTime utcNow)
        {
            if (_lastWarningSentAt.TryGetValue(contractId, out var lastSentAt) && utcNow - lastSentAt < WarningCooldown)
                return false;

            _lastWarningSentAt[contractId] = utcNow;
            return true;
        }

    }
}
