namespace CorpServe.Shared.DTOs.ChatDTOs
{
    public class SendMessageDTO
    {
        public string ChatRoomId { get; set; } = default!;
        public string Content { get; set; } = default!;
        public int Type { get; set; } = 1;
        public string? MediaUrl { get; set; }
        public string? MediaMimeType { get; set; }
        public long? MediaSizeBytes { get; set; }
    }
}
