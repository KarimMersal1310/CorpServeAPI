namespace CorpServe.Shared.DTOs.PaymentDTOs
{
    public class PaymentSummaryDTO
    {
        public string PaymentId { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public string MerchantOrderId { get; set; } = default!;
        public string PaymentStatus { get; set; } = default!;
        public string PayoutStatus { get; set; } = default!;
        public decimal Amount { get; set; }
        public decimal Commision { get; set; }
        public decimal VendorNetAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PayoutReference { get; set; }
        public DateTime? PayoutCompletedAt { get; set; }
        public string? CheckoutUrl { get; set; }
    }
}
