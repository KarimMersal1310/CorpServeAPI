using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.NotificationModule;
using CorpServe.Services.Specifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class NotificationCleanupBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
        private const int RetentionDays = 3;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationCleanupBackgroundService> _logger;

        public NotificationCleanupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<NotificationCleanupBackgroundService> logger)
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
                    await CleanupNotificationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup old notifications.");
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

        private async Task CleanupNotificationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var notificationRepo = unitOfWork.GetRepository<SystemNotification, string>();

            var cutoffUtc = DateTime.UtcNow.AddDays(-RetentionDays);
            var expiredNotifications = (await notificationRepo.GetAllAsync(new NotificationsOlderThanSpecification(cutoffUtc))).ToList();
            if (expiredNotifications.Count == 0)
                return;

            foreach (var notification in expiredNotifications)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                notificationRepo.Remove(notification);
            }

            await unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Notification cleanup removed {Count} notification(s) older than {RetentionDays} day(s).", expiredNotifications.Count, RetentionDays);
        }
    }
}
