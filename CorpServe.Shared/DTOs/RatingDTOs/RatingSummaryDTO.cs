namespace CorpServe.Shared.DTOs.RatingDTOs
{
    public class RatingSummaryDTO
    {
        public string RequestId { get; set; } = default!;
        public string PaymentId { get; set; } = default!;
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
