namespace CorpServe.Shared.DTOs.ChatDTOs
{
    public class ChatRoomDTO
    {
        public string Id { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string? ClientProfilePictureUrl { get; set; }
        public string? VendorProfilePictureUrl { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}
