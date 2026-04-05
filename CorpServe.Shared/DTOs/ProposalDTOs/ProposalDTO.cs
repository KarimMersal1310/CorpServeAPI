namespace CorpServe.Shared.DTOs.ProposalDTOs
{
    public class ProposalDTO
    {
        public string Id { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string ProposalStatus { get; set; } = default!;
        public string ProposalType { get; set; } = default!;
        public decimal? ProposedPrice { get; set; }
        public DateTime? ProposedDeadline { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
