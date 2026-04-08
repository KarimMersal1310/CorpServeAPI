using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace CorpServe.Services.Payments
{
    public class PaymobClient : IPaymobClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private const int MaxErrorDetailLength = 2000;

        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymobClient> _logger;

        public PaymobClient(HttpClient httpClient, ILogger<PaymobClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PaymobIntentionCreateResult> CreateIntentionAsync(PaymobIntentionRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync("v1/intention/", request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Paymob intention request failed. StatusCode: {StatusCode}, Body: {Body}", (int)response.StatusCode, body);
                return new PaymobIntentionCreateResult
                {
                    ErrorDetail = TruncateForClient($"Paymob returned HTTP {(int)response.StatusCode}: {body}"),
                };
            }

            PaymobIntentionResponse? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<PaymobIntentionResponse>(body, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Paymob intention JSON parse failed. Body: {Body}", body);
                return new PaymobIntentionCreateResult
                {
                    ErrorDetail = "Paymob returned success but the response could not be parsed as an intention.",
                };
            }

            if (parsed is null
                || string.IsNullOrWhiteSpace(parsed.Id)
                || string.IsNullOrWhiteSpace(parsed.ClientSecret))
            {
                _logger.LogWarning("Paymob intention response missing id or client_secret. Body: {Body}", body);
                return new PaymobIntentionCreateResult
                {
                    ErrorDetail = TruncateForClient($"Paymob response did not include id and client_secret. Body: {body}"),
                };
            }

            return new PaymobIntentionCreateResult { Response = parsed };
        }

        private static string TruncateForClient(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= MaxErrorDetailLength
                ? value
                : value[..MaxErrorDetailLength] + "…";
        }
    }
}
