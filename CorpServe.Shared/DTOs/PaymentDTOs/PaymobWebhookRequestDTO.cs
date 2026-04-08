namespace CorpServe.Shared.DTOs.PaymentDTOs
{
    public class PaymobWebhookRequestDTO
    {
        public string Payload { get; set; } = default!;
        public string? Hmac { get; set; }
    }
}
