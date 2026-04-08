namespace CorpServe.Shared.DTOs.RatingDTOs
{
    public class RatingRequirementDTO
    {
        public string RequestId { get; set; } = default!;
        public string PaymentId { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public bool IsRequired { get; set; }
    }
}
