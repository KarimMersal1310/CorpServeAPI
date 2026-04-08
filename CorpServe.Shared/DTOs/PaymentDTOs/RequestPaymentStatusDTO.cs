namespace CorpServe.Shared.DTOs.PaymentDTOs
{
    public class RequestPaymentStatusDTO
    {
        public string RequestId { get; set; } = default!;
        public string? PaymentId { get; set; }
        public string PaymentStatus { get; set; } = default!;
        public string PayoutStatus { get; set; } = default!;
        public decimal Amount { get; set; }
        public decimal Commision { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal VendorNetAmount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? FailureReason { get; set; }
        public string? PayoutReference { get; set; }
        public DateTime? PayoutCompletedAt { get; set; }
        public string? PayoutFailureReason { get; set; }
        public string? CheckoutUrl { get; set; }
    }
}
