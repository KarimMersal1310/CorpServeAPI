namespace CorpServe.Shared.DTOs.ProposalDTOs
{
    public class ActiveRequestDTO
    {
        public string RequestId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }
        public DateTime Deadline { get; set; }
        public int ProgressPercentage { get; set; }
        public string? ClientName { get; set; }
        public string? VendorName { get; set; }
    }
}
