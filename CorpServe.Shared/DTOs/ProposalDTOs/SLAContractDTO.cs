namespace CorpServe.Shared.DTOs.ProposalDTOs
{
    public class SLAContractDTO
    {
        public string Id { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public string ProposalId { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string VendorId { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string? ClientProfilePictureUrl { get; set; }
        public string? VendorProfilePictureUrl { get; set; }
        public decimal ContractPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Deadline { get; set; }
        public string SLAStatus { get; set; } = default!;
        public string WarningLevel { get; set; } = default!;
        public double RemainingHours { get; set; }
        public bool IsWarning { get; set; }
    }
}
