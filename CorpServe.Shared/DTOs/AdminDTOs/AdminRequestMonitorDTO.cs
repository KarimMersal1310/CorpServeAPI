namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminRequestMonitorDTO
    {
        public string RequestId { get; set; } = default!;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public DateTime? Deadline { get; set; }
        public int Progress { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
        public string? SlaStatus { get; set; }
        public int NumberOfProposals { get; set; }
        public IEnumerable<AdminRequestProposalDTO> Proposals { get; set; } = new List<AdminRequestProposalDTO>();
    }
}
