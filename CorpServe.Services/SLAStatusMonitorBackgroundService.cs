using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Specifications;
using CorpServe.Shared.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class SLAStatusMonitorBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);
        /// <summary>When remaining time is within this window, SLA transitions from Inprogress to Breached.</summary>
        private static readonly TimeSpan BreachWindow = TimeSpan.FromHours(48);
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
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var adminMonitorService = scope.ServiceProvider.GetRequiredService<IAdminMonitorService>();

            var contracts = (await slaRepository.GetAllAsync(new ActiveSlaContractsForMonitoringSpecification())).ToList();
            if (contracts.Count == 0)
                return;

            var hasChanges = false;
            var utcNow = DateTime.UtcNow;
            var vendorCounterUpdated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var contract in contracts)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (contract.Request.RequestStatus == RequestStatus.Completed)
                {
                    var wasNotCompleted = contract.SLAStatus != SLAStatus.Completed;
                    var completedOnTime = contract.Deadline >= utcNow || contract.SLAStatus == SLAStatus.Inprogress || contract.SLAStatus == SLAStatus.Breached;

                    if (wasNotCompleted)
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

                        if (completedOnTime && !vendorCounterUpdated.Contains(contract.VendorId))
                        {
                            await ResetVendorDelayCounterAsync(userManager, contract.VendorId);
                            vendorCounterUpdated.Add(contract.VendorId);
                        }
                    }

                    continue;
                }

                var isSuspendedContract = contract.Client.Status == UserStatus.Suspended || contract.Vendor.Status == UserStatus.Suspended;
                if (isSuspendedContract)
                {
                    if (contract.SLAStatus == SLAStatus.Inprogress || contract.SLAStatus == SLAStatus.Breached)
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
                    var transitionedToDelayed = contract.SLAStatus != SLAStatus.Delayed;
                    if (transitionedToDelayed)
                    {
                        contract.SLAStatus = SLAStatus.Delayed;
                        slaRepository.Update(contract);
                        hasChanges = true;

                        await IncrementVendorDelayCounterAsync(
                            userManager, adminMonitorService, notificationService,
                            contract.VendorId, contract.RequestId);
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

                if (remaining <= BreachWindow
                    && contract.SLAStatus == SLAStatus.Inprogress)
                {
                    contract.SLAStatus = SLAStatus.Breached;
                    slaRepository.Update(contract);
                    hasChanges = true;

                    if (ShouldSendWarning(contract.Id, utcNow))
                    {
                        await NotifyInAppAsync(
                            notificationService,
                            [contract.ClientId, contract.VendorId],
                            NotificationTitles.SlaBreached,
                            $"SLA for request '{contract.Request.Title}' is breached — less than 48 hours until deadline.",
                            NotificationTypes.Warning,
                            contract.RequestId,
                            "Request");
                    }

                    continue;
                }
            }

            if (hasChanges)
                await unitOfWork.SaveChangesAsync();

            _logger.LogInformation("SLA monitor checked {ContractsCount} contract(s).", contracts.Count);
        }

        private async Task IncrementVendorDelayCounterAsync(
            UserManager<ApplicationUser> userManager,
            IAdminMonitorService adminMonitorService,
            INotificationService notificationService,
            string vendorId,
            string relatedRequestId)
        {
            try
            {
                var vendor = await userManager.FindByIdAsync(vendorId);
                if (vendor is null || vendor.Status == UserStatus.Suspended)
                    return;

                vendor.ConsecutiveDelayedSlaCount++;
                await userManager.UpdateAsync(vendor);

                if (vendor.ConsecutiveDelayedSlaCount == 3)
                {
                    await notificationService.SendNotificationAsync(
                        vendorId,
                        NotificationTitles.SlaStreakWarning,
                        "Warning: You have 3 consecutive delayed SLA contracts. One more will result in account suspension.",
                        NotificationTypes.Warning,
                        relatedRequestId,
                        "Request",
                        sendEmail: false);
                }
                else if (vendor.ConsecutiveDelayedSlaCount >= 4)
                {
                    const string reason = "Your account was suspended because 4 consecutive SLA contracts became Delayed.";
                    await adminMonitorService.SuspendUserAsync(vendorId, reason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update consecutive delay counter for vendor {VendorId}.", vendorId);
            }
        }

        private async Task ResetVendorDelayCounterAsync(UserManager<ApplicationUser> userManager, string vendorId)
        {
            try
            {
                var vendor = await userManager.FindByIdAsync(vendorId);
                if (vendor is null || vendor.ConsecutiveDelayedSlaCount == 0)
                    return;

                vendor.ConsecutiveDelayedSlaCount = 0;
                await userManager.UpdateAsync(vendor);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reset delay counter for vendor {VendorId}.", vendorId);
            }
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
