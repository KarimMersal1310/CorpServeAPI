namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class VendorRequestViewDTO
    {
        public string RequestId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string RequestCategory { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string? ClientProfilePictureUrl { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string CreatedAt => ToTimeAgo(CreatedAtUtc);
        public string Description { get; set; } = default!;
        public decimal BudgetMin { get; set; }
        public decimal BudgetMax { get; set; }
        public DateTime Deadline { get; set; }
        public ICollection<RequestAttachmentDTO> RequestAttachments { get; set; } = new List<RequestAttachmentDTO>();

        private static string ToTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.UtcNow - date;
            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.Seconds} seconds ago";
            if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.Minutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{timeSpan.Hours} hours ago";
            if (timeSpan.TotalDays < 30)
                return $"{timeSpan.Days} days ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }
    }
}
