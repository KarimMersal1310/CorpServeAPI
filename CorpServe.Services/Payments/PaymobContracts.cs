using System.Text.Json.Serialization;

namespace CorpServe.Services.Payments
{
    public class PaymobIntentionRequest
    {
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "EGP";

        [JsonPropertyName("special_reference")]
        public string SpecialReference { get; set; } = default!;

        [JsonPropertyName("notification_url")]
        public string NotificationUrl { get; set; } = default!;

        [JsonPropertyName("redirection_url")]
        public string RedirectionUrl { get; set; } = default!;

        [JsonPropertyName("payment_methods")]
        public List<int> PaymentMethods { get; set; } = [];

        [JsonPropertyName("billing_data")]
        public Dictionary<string, string> BillingData { get; set; } = [];

        [JsonPropertyName("items")]
        public List<PaymobItem> Items { get; set; } = [];

        [JsonPropertyName("extras")]
        public Dictionary<string, string> Extras { get; set; } = [];
    }

    public class PaymobItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = default!;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class PaymobIntentionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("client_secret")]
        public string? ClientSecret { get; set; }
    }

    /// <summary>Result of POST intention; includes Paymob error body when the call fails.</summary>
    public sealed class PaymobIntentionCreateResult
    {
        public PaymobIntentionResponse? Response { get; init; }
        public string? ErrorDetail { get; init; }

        public bool IsSuccess =>
            Response is not null
            && !string.IsNullOrWhiteSpace(Response.Id)
            && !string.IsNullOrWhiteSpace(Response.ClientSecret);
    }
}
