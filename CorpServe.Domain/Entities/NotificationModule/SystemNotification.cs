using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities;

namespace CorpServe.Domain.Entities.NotificationModule
{
    public class SystemNotification : BaseEntity<string>
    {
        public string RecipientId { get; set; } = default!;
        public ApplicationUser Recipient { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
    }
}
