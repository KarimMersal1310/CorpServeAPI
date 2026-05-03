namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminRequestProposalDTO
    {
        public string ProposalId { get; set; } = default!;
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = string.Empty;
        public string? VendorProfilePictureUrl { get; set; }
        public string ProposalStatus { get; set; } = string.Empty;
        public string ProposalType { get; set; } = string.Empty;
        public decimal? ProposedPrice { get; set; }
        public DateTime? ProposedDeadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Message { get; set; }
    }
}
