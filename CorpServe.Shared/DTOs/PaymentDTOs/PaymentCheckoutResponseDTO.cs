namespace CorpServe.Shared.DTOs.PaymentDTOs
{
    public class PaymentCheckoutResponseDTO
    {
        public string PaymentId { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string MerchantOrderId { get; set; } = default!;
        public string PaymentStatus { get; set; } = default!;
        public decimal Amount { get; set; }
        public decimal Commision { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal VendorNetAmount { get; set; }
        public string PayoutStatus { get; set; } = default!;
        public string CheckoutUrl { get; set; } = default!;
    }
}
