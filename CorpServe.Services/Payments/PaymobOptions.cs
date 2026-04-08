using System.ComponentModel.DataAnnotations;

namespace CorpServe.Services.Payments
{
    public class PaymobOptions
    {
        [Required]
        public string Mode { get; set; } = "Test";

        [Required]
        /// <summary>Paymob Accept API host (no /api suffix). Intention POST is {BaseUrl}/v1/intention/.</summary>
        public string BaseUrl { get; set; } = "https://accept.paymob.com";

        [Required]
        public string SecretKey { get; set; } = default!;

        [Required]
        public string PublicKey { get; set; } = default!;

        [Required]
        public string WebhookHmacSecret { get; set; } = default!;

        [Required]
        public string WebhookUrl { get; set; } = default!;

        [Required]
        public string SuccessRedirectUrl { get; set; } = default!;

        [Required]
        public string FailureRedirectUrl { get; set; } = default!;

        public string Currency { get; set; } = "EGP";

        public int TimeoutSeconds { get; set; } = 30;

        public List<int> PaymentMethodIntegrationIds { get; set; } = [];

        public string? HostedCheckoutBaseUrl { get; set; }
    }
}
