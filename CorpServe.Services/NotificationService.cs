using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.NotificationModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Specifications;
using CorpServe.Shared;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.NotificationDTOs;
using CorpServe.Shared.QueryParams;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealtimeNotifier _realtimeNotifier;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IUnitOfWork unitOfWork, IRealtimeNotifier realtimeNotifier, ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _realtimeNotifier = realtimeNotifier;
            _logger = logger;
        }

        public async Task<Result<bool>> SendNotificationAsync(string recipientId, string title, string message, string type, string? relatedEntityId = null, string? relatedEntityType = null)
        {
            if (string.IsNullOrWhiteSpace(recipientId))
                return Error.Validation("Notification.RecipientRequired", "Recipient is required.");

            var parsedType = ParseNotificationType(type);
            if (parsedType is null)
                return Error.Validation("Notification.InvalidType", "Notification type is invalid.");

            if (string.IsNullOrWhiteSpace(title))
                return Error.Validation("Notification.TitleRequired", "Notification title is required.");

            if (string.IsNullOrWhiteSpace(message))
                return Error.Validation("Notification.MessageRequired", "Notification message is required.");

            var notificationRepo = _unitOfWork.GetRepository<SystemNotification, string>();
            var notification = new SystemNotification
            {
                RecipientId = recipientId,
                Title = title.Trim(),
                Message = message.Trim(),
                Type = parsedType.Value,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = relatedEntityId?.Trim(),
                RelatedEntityType = relatedEntityType?.Trim()
            };

            await notificationRepo.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var dto = Map(notification);
            await TryRealtimeNotifyAsync(recipientId, dto);
            return true;
        }

        public async Task<Result<bool>> SendNotificationToManyAsync(IEnumerable<string> recipientIds, string title, string message, string type, string? relatedEntityId = null, string? relatedEntityType = null)
        {
            var recipients = recipientIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipients.Count == 0)
                return Error.Validation("Notification.RecipientsRequired", "At least one recipient is required.");

            var parsedType = ParseNotificationType(type);
            if (parsedType is null)
                return Error.Validation("Notification.InvalidType", "Notification type is invalid.");

            if (string.IsNullOrWhiteSpace(title))
                return Error.Validation("Notification.TitleRequired", "Notification title is required.");

            if (string.IsNullOrWhiteSpace(message))
                return Error.Validation("Notification.MessageRequired", "Notification message is required.");

            var notificationRepo = _unitOfWork.GetRepository<SystemNotification, string>();
            var notifications = recipients.Select(recipientId => new SystemNotification
            {
                RecipientId = recipientId,
                Title = title.Trim(),
                Message = message.Trim(),
                Type = parsedType.Value,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = relatedEntityId?.Trim(),
                RelatedEntityType = relatedEntityType?.Trim()
            }).ToList();

            foreach (var notification in notifications)
            {
                await notificationRepo.AddAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync();

            foreach (var notification in notifications)
            {
                await TryRealtimeNotifyAsync(notification.RecipientId, Map(notification));
            }

            return true;
        }

        public async Task<PaginatedResult<NotificationDTO>> GetUserNotificationsAsync(string userId, NotificationQueryParams queryParams)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new PaginatedResult<NotificationDTO>(queryParams.PageIndex, queryParams.PageSize, 0, []);

            var notificationRepo = _unitOfWork.GetRepository<SystemNotification, string>();
            var listSpecification = new UserNotificationsListSpecification(userId, queryParams.Search, queryParams.IsRead, queryParams.PageSize, queryParams.PageIndex);
            var countSpecification = new UserNotificationsCountSpecification(userId, queryParams.Search, queryParams.IsRead);

            var notifications = await notificationRepo.GetAllAsync(listSpecification);
            var count = await notificationRepo.CountAsync(countSpecification);
            var data = notifications.Select(Map).ToList();

            return new PaginatedResult<NotificationDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
        }

        public async Task<Result<int>> GetUnreadCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Notification.UserRequired", "User identity is required.");

            var notificationRepo = _unitOfWork.GetRepository<SystemNotification, string>();
            var count = await notificationRepo.CountAsync(new UserNotificationsCountSpecification(userId, null, false));
            return count;
        }

        public async Task<Result<bool>> MarkAsReadAsync(string userId, string notificationId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Notification.UserRequired", "User identity is required.");

            if (string.IsNullOrWhiteSpace(notificationId))
                return Error.Validation("Notification.IdRequired", "Notification id is required.");

            var notificationRepo = _unitOfWork.GetRepository<SystemNotification, string>();
            var notification = await notificationRepo.GetByIdAsync(new UserNotificationByIdSpecification(userId, notificationId));
            if (notification is null)
                return Error.NotFound("Notification.NotFound", "Notification not found.");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notificationRepo.Update(notification);
                await _unitOfWork.SaveChangesAsync();
            }

            return true;
        }

        public async Task<Result<bool>> MarkAllAsReadAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Notification.UserRequired", "User identity is required.");

            await _unitOfWork.MarkAllNotificationsAsReadAsync(userId);
            return true;
        }

        private async Task TryRealtimeNotifyAsync(string userId, NotificationDTO notification)
        {
            try
            {
                await _realtimeNotifier.NotifyUserAsync(userId, notification);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push realtime notification to user {UserId}.", userId);
            }
        }

        private static NotificationType? ParseNotificationType(string type)
        {
            if (Enum.TryParse<NotificationType>(type, true, out var parsed))
                return parsed;

            return null;
        }

        private static NotificationDTO Map(SystemNotification notification)
        {
            return new NotificationDTO
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                RelatedEntityId = notification.RelatedEntityId,
                RelatedEntityType = notification.RelatedEntityType,
                NavigateUrl = BuildNavigateUrl(notification.RelatedEntityType, notification.RelatedEntityId)
            };
        }

        private static string BuildNavigateUrl(string? relatedEntityType, string? relatedEntityId)
        {
            if (string.IsNullOrWhiteSpace(relatedEntityType))
                return string.Empty;

            var id = relatedEntityId?.Trim();

            return relatedEntityType.Trim() switch
            {
                "Request" when !string.IsNullOrWhiteSpace(id) => $"/requests/{id}",
                "Proposal" when !string.IsNullOrWhiteSpace(id) => $"/proposals/{id}",
                "VendorVerification" when !string.IsNullOrWhiteSpace(id) => $"/vendor-verification/{id}",
                "SLAContract" when !string.IsNullOrWhiteSpace(id) => $"/proposals/request/{id}/sla",
                "Payment" => "/payments",
                "Rating" when !string.IsNullOrWhiteSpace(id) => $"/payments?requestId={id}",
                "User" => "/profile",
                _ => string.Empty
            };
        }
    }
}
