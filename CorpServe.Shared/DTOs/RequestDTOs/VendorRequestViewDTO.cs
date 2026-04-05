namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class VendorRequestViewDTO
    {
        public string RequestId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string RequestCategory { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string CreatedAt { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal BudgetMin { get; set; }
        public decimal BudgetMax { get; set; }
        public DateTime Deadline { get; set; }
        public ICollection<RequestAttachmentDTO> RequestAttachments { get; set; } = new List<RequestAttachmentDTO>();
    }
}
