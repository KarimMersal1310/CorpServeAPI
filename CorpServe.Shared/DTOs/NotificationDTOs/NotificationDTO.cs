namespace CorpServe.Shared.DTOs.NotificationDTOs
{
    public class NotificationDTO
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string Type { get; set; } = default!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public string NavigateUrl { get; set; } = string.Empty;
    }
}
