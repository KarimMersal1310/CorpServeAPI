using CorpServe.Shared;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.NotificationDTOs;
using CorpServe.Shared.QueryParams;

namespace CorpServe.Services.Abstraction
{
    public interface INotificationService
    {
        Task<Result<bool>> SendNotificationAsync(string recipientId, string title, string message, string type, string? relatedEntityId = null, string? relatedEntityType = null);
        Task<Result<bool>> SendNotificationToManyAsync(IEnumerable<string> recipientIds, string title, string message, string type, string? relatedEntityId = null, string? relatedEntityType = null);
        Task<PaginatedResult<NotificationDTO>> GetUserNotificationsAsync(string userId, NotificationQueryParams queryParams);
        Task<Result<int>> GetUnreadCountAsync(string userId);
        Task<Result<bool>> MarkAsReadAsync(string userId, string notificationId);
        Task<Result<bool>> MarkAllAsReadAsync(string userId);
    }
}
