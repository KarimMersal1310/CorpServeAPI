namespace CorpServe.Shared.DTOs.ChatDTOs
{
    public class MessageDTO
    {
        public string Id { get; set; } = default!;
        public string ChatRoomId { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Sender { get; set; } = default!;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaMimeType { get; set; }
        public long? MediaSizeBytes { get; set; }
    }
}
