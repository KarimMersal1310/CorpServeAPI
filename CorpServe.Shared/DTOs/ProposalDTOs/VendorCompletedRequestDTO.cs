namespace CorpServe.Shared.DTOs.ProposalDTOs
{
    public class VendorCompletedRequestDTO
    {
        public string RequestId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime CompletedAt { get; set; }
        public int Rating { get; set; }
        public string? Feedback { get; set; }
        public string PaymentStatus { get; set; } = default!;
        public string PayoutStatus { get; set; } = default!;
    }
}
