using CorpServe.Shared.DTOs.AIEstimationDTOs;

namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class RequestDTO
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string CategoryId { get; set; } = default!;
        public string CategoryName { get; set; } = default!;
        public decimal BudgetMin { get; set; }
        public decimal BudgetMax { get; set; }
        public DateTime ExpectedDeadline { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string CreatedAt => ToTimeAgo(CreatedAtUtc);
        public int ProgressPercentage { get; set; }
        public string RequestStatus { get; set; } = default!;
        /// <summary>Optional vendor assigned via SLA or progress (for client views).</summary>
        public string? AssignedVendorId { get; set; }
        public string? AssignedVendorName { get; set; }
        public string? VendorProfilePictureUrl { get; set; }
        public AIEstimationDTO? AIEstimation { get; set; }
        public ICollection<RequestAttachmentDTO> RequestAttachments { get; set; } = new List<RequestAttachmentDTO>();

        /// <summary>Client rating (1–5) after service completion, when submitted.</summary>
        public int? RatingStars { get; set; }

        /// <summary>Optional written feedback with the rating.</summary>
        public string? RatingComment { get; set; }

        /// <summary>Total amount paid by the client (including commission) when payment is completed.</summary>
        public decimal? PaidTotalAmount { get; set; }

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
