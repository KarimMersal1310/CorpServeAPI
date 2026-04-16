namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminRequestMonitorDTO
    {
        public string RequestId { get; set; } = default!;
        public string ClientId { get; set; } = string.Empty;
        public string? VendorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public string? ClientProfilePictureUrl { get; set; }
        public string? VendorProfilePictureUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        /// <summary>Client-requested budget range (from the request, not contract/proposal price).</summary>
        public decimal BudgetMin { get; set; }
        public decimal BudgetMax { get; set; }
        public DateTime? Deadline { get; set; }
        public int Progress { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
        /// <summary>SLAStatus enum name when an SLA contract exists; null when none.</summary>
        public string? SlaStatus { get; set; }
        public int NumberOfProposals { get; set; }
        public IEnumerable<AdminRequestProposalDTO> Proposals { get; set; } = new List<AdminRequestProposalDTO>();
    }
}
