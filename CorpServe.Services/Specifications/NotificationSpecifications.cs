using CorpServe.Domain.Entities.NotificationModule;

namespace CorpServe.Services.Specifications
{
    public sealed class UserNotificationsListSpecification : BaseSpecificactions<SystemNotification, string>
    {
        public UserNotificationsListSpecification(string userId, string? search, bool? isRead, int pageSize, int pageIndex)
            : base(n => n.RecipientId == userId
                && (string.IsNullOrWhiteSpace(search)
                    || n.Title.Contains(search)
                    || n.Message.Contains(search))
                && (!isRead.HasValue || n.IsRead == isRead.Value))
        {
            AddOrderByDescending(n => n.CreatedAt);
            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class UserNotificationsCountSpecification : BaseSpecificactions<SystemNotification, string>
    {
        public UserNotificationsCountSpecification(string userId, string? search, bool? isRead)
            : base(n => n.RecipientId == userId
                && (string.IsNullOrWhiteSpace(search)
                    || n.Title.Contains(search)
                    || n.Message.Contains(search))
                && (!isRead.HasValue || n.IsRead == isRead.Value))
        {
        }
    }

    public sealed class UserNotificationByIdSpecification : BaseSpecificactions<SystemNotification, string>
    {
        public UserNotificationByIdSpecification(string userId, string notificationId)
            : base(n => n.RecipientId == userId && n.Id == notificationId)
        {
        }
    }

    public sealed class NotificationsOlderThanSpecification : BaseSpecificactions<SystemNotification, string>
    {
        public NotificationsOlderThanSpecification(DateTime cutoffUtc)
            : base(n => n.CreatedAt <= cutoffUtc)
        {
        }
    }
}
