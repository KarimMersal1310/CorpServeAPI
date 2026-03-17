using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CorpServe.Services
{
    public class AIEstimationService : IAIEstimationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AIEstimationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<Result<AIEstimationDTO>> GenerateEstimateAsync(GenerateRequestEstimateDTO estimateDTO)
        {
            var apiKey = _configuration["AISettings:ApiKey"];
            var provider = (_configuration["AISettings:Provider"] ?? string.Empty).Trim();
            var hasGoogleApiKey = !string.IsNullOrWhiteSpace(apiKey)
                && apiKey.StartsWith("AIza", StringComparison.OrdinalIgnoreCase);
            var isGeminiProvider = provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase)
                || (string.IsNullOrWhiteSpace(provider) && hasGoogleApiKey);
            var model = _configuration["AISettings:Model"] ?? (isGeminiProvider ? "gemini-1.5-flash" : "gpt-4o-mini");
            var endpoint = _configuration["AISettings:Endpoint"]
                ?? (isGeminiProvider || hasGoogleApiKey
                    ? "https://generativelanguage.googleapis.com/v1/models/{model}:generateContent"
                    : "https://api.openai.com/v1/chat/completions");

            if (string.IsNullOrWhiteSpace(apiKey))
                return Error.Failure("AI.ConfigurationMissing", "AI API key is missing.");

            var prompt = BuildPrompt(estimateDTO);

            var isGemini = isGeminiProvider
                || hasGoogleApiKey
                || endpoint.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase);

            object requestBody = isGemini
                ? new
                {
                    contents = new object[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2
                    }
                }
                : new
                {
                    model,
                    messages = new object[]
                    {
                        new { role = "system", content = "You are a software procurement estimation assistant. Return only valid JSON." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.2,
                    response_format = new { type = "json_object" }
                };

            var requestUrl = endpoint;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            string responseContent = string.Empty;

            if (isGemini)
            {
                var preferredModels = new[] { model, "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-flash" }
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var endpointTemplates = BuildGeminiEndpointTemplates(endpoint);
                var availableModels = await GetAvailableGeminiModelsAsync(endpointTemplates, apiKey);
                var hasDiscoveredModels = availableModels.Count > 0;
                var discoveredSet = new HashSet<string>(availableModels, StringComparer.OrdinalIgnoreCase);

                var candidateModels = new List<string>();
                foreach (var preferredModel in preferredModels)
                {
                    if (!hasDiscoveredModels || discoveredSet.Contains(preferredModel))
                        candidateModels.Add(preferredModel);
                }

                if (hasDiscoveredModels)
                    candidateModels.AddRange(availableModels);

                candidateModels = candidateModels
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var candidateRequests = endpointTemplates
                    .SelectMany(template => candidateModels.Select(candidateModel => new { template, candidateModel }))
                    .ToArray();

                for (var i = 0; i < candidateRequests.Length; i++)
                {
                    var candidate = candidateRequests[i];
                    requestUrl = BuildGeminiEndpoint(candidate.template, candidate.candidateModel);
                    requestUrl = AppendApiKeyQuery(requestUrl, apiKey);

                    const int maxTransientRetries = 2;
                    for (var attempt = 0; attempt <= maxTransientRetries; attempt++)
                    {
                        using var geminiRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                        geminiRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8);
                        geminiRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                        using var geminiResponse = await _httpClient.SendAsync(geminiRequest);
                        responseContent = await geminiResponse.Content.ReadAsStringAsync();
                        statusCode = geminiResponse.StatusCode;

                        if (geminiResponse.IsSuccessStatusCode)
                            break;

                        var isTransient = IsTransientGeminiError(geminiResponse.StatusCode, responseContent);
                        var isLastAttempt = attempt == maxTransientRetries;
                        if (!isTransient || isLastAttempt)
                            break;

                        await Task.Delay((attempt + 1) * 500);
                    }

                    if ((int)statusCode is >= 200 and < 300)
                        break;

                    // Retry with fallback models/API versions when a model is unavailable on a specific version.
                    var isLastCandidate = i == candidateRequests.Length - 1;
                    if (!ShouldTryAnotherGeminiModel(statusCode, responseContent) || isLastCandidate)
                        break;
                }
            }
            else
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using var response = await _httpClient.SendAsync(request);
                responseContent = await response.Content.ReadAsStringAsync();
                statusCode = response.StatusCode;
            }

            if ((int)statusCode < 200 || (int)statusCode >= 300)
            {
                var providerMessage = string.IsNullOrWhiteSpace(responseContent)
                    ? "No response body returned from AI provider."
                    : responseContent;

                return Error.Failure(
                    "AI.RequestFailed",
                    $"Failed to generate AI estimate. StatusCode: {(int)statusCode}. ProviderResponse: {providerMessage}");
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var content = isGemini
                    ? doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString()
                    : doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                if (string.IsNullOrWhiteSpace(content))
                    return Error.Failure("AI.EmptyResponse", "AI returned an empty response.");

                var normalizedJson = NormalizeJsonResponse(content);
                using var estimateJson = JsonDocument.Parse(normalizedJson);
                var root = estimateJson.RootElement;

                var result = new AIEstimationDTO
                {
                    EstimatedCost = root.GetProperty("estimatedCost").GetDecimal(),
                    EstimatedTime = DateTime.UtcNow.AddDays(root.GetProperty("estimatedDays").GetInt32()),
                    Confidence = root.GetProperty("confidence").GetInt32()
                };

                return result;
            }
            catch
            {
                return Error.Failure("AI.ParseFailed", "Failed to parse AI estimation response.");
            }
        }

        private static string BuildPrompt(GenerateRequestEstimateDTO estimateDTO)
        {
            return $@"Generate implementation estimate for this service request.

Input:
- Title: {estimateDTO.Title}
- Description: {estimateDTO.Description}
- CategoryId: {estimateDTO.CategoryId}
- BudgetMin: {estimateDTO.BudgetMin}
- BudgetMax: {estimateDTO.BudgetMax}
- ExpectedDeadline: {estimateDTO.ExpectedDeadline:O}

Return only JSON with this exact schema:
{{
  ""estimatedCost"": number,
  ""estimatedDays"": number,
  ""confidence"": number
}}

Rules:
- estimatedCost must be between BudgetMin and BudgetMax.
- estimatedDays must be positive and not exceed days until ExpectedDeadline.
- confidence must be from 0 to 100.";
        }

        private static string BuildGeminiEndpoint(string endpointTemplate, string model)
        {
            if (endpointTemplate.Contains("{model}", StringComparison.OrdinalIgnoreCase))
                return endpointTemplate.Replace("{model}", Uri.EscapeDataString(model), StringComparison.OrdinalIgnoreCase);

            const string modelsSegment = "/models/";
            const string actionSegment = ":generateContent";

            var modelsIndex = endpointTemplate.IndexOf(modelsSegment, StringComparison.OrdinalIgnoreCase);
            var actionIndex = endpointTemplate.IndexOf(actionSegment, StringComparison.OrdinalIgnoreCase);

            if (modelsIndex >= 0 && actionIndex > modelsIndex)
            {
                var start = modelsIndex + modelsSegment.Length;
                return endpointTemplate.Remove(start, actionIndex - start)
                    .Insert(start, Uri.EscapeDataString(model));
            }

            return endpointTemplate;
        }

        private static string[] BuildGeminiEndpointTemplates(string endpoint)
        {
            var templates = new List<string>();
            if (!string.IsNullOrWhiteSpace(endpoint))
                templates.Add(endpoint);

            if (endpoint.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            {
                if (endpoint.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
                    templates.Add(endpoint.Replace("/v1/", "/v1beta/", StringComparison.OrdinalIgnoreCase));
                else if (endpoint.Contains("/v1beta/", StringComparison.OrdinalIgnoreCase))
                    templates.Add(endpoint.Replace("/v1beta/", "/v1/", StringComparison.OrdinalIgnoreCase));
            }

            return templates
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private async Task<List<string>> GetAvailableGeminiModelsAsync(IEnumerable<string> endpointTemplates, string apiKey)
        {
            var discoveredModels = new List<string>();

            foreach (var endpointTemplate in endpointTemplates)
            {
                var listModelsUrl = BuildGeminiListModelsUrl(endpointTemplate);
                if (string.IsNullOrWhiteSpace(listModelsUrl))
                    continue;

                listModelsUrl = AppendApiKeyQuery(listModelsUrl, apiKey);

                using var request = new HttpRequestMessage(HttpMethod.Get, listModelsUrl);
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    continue;

                var responseContent = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    if (!doc.RootElement.TryGetProperty("models", out var modelsElement) ||
                        modelsElement.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var modelElement in modelsElement.EnumerateArray())
                    {
                        if (!modelElement.TryGetProperty("name", out var nameElement))
                            continue;

                        var fullName = nameElement.GetString();
                        if (string.IsNullOrWhiteSpace(fullName))
                            continue;

                        if (!SupportsGenerateContent(modelElement))
                            continue;

                        const string prefix = "models/";
                        var modelName = fullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                            ? fullName[prefix.Length..]
                            : fullName;

                        if (!string.IsNullOrWhiteSpace(modelName))
                            discoveredModels.Add(modelName);
                    }
                }
                catch
                {
                    // Ignore discovery parse failures and continue with fallback models.
                }
            }

            return discoveredModels
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool SupportsGenerateContent(JsonElement modelElement)
        {
            if (!modelElement.TryGetProperty("supportedGenerationMethods", out var methodsElement) ||
                methodsElement.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var methodElement in methodsElement.EnumerateArray())
            {
                if (methodElement.ValueKind == JsonValueKind.String &&
                    methodElement.GetString()?.Equals("generateContent", StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }

            return false;
        }

        private static string BuildGeminiListModelsUrl(string endpointTemplate)
        {
            var actionIndex = endpointTemplate.IndexOf(":generateContent", StringComparison.OrdinalIgnoreCase);
            if (actionIndex < 0)
                return string.Empty;

            var beforeAction = endpointTemplate[..actionIndex];
            var modelsIndex = beforeAction.IndexOf("/models/", StringComparison.OrdinalIgnoreCase);
            if (modelsIndex < 0)
                return string.Empty;

            return beforeAction[..(modelsIndex + "/models".Length)];
        }

        private static string AppendApiKeyQuery(string url, string apiKey)
        {
            if (url.Contains("key=", StringComparison.OrdinalIgnoreCase))
                return url;

            return url.Contains('?')
                ? $"{url}&key={Uri.EscapeDataString(apiKey)}"
                : $"{url}?key={Uri.EscapeDataString(apiKey)}";
        }

        private static bool ShouldTryAnotherGeminiModel(HttpStatusCode statusCode, string providerResponse)
        {
            if (statusCode == HttpStatusCode.NotFound
                || statusCode == HttpStatusCode.TooManyRequests
                || statusCode == HttpStatusCode.ServiceUnavailable
                || statusCode == HttpStatusCode.InternalServerError
                || statusCode == HttpStatusCode.BadGateway
                || statusCode == HttpStatusCode.GatewayTimeout)
                return true;

            var response = providerResponse ?? string.Empty;
            return response.Contains("quota", StringComparison.OrdinalIgnoreCase)
                || response.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || response.Contains("not supported", StringComparison.OrdinalIgnoreCase)
                || response.Contains("permission", StringComparison.OrdinalIgnoreCase)
                || response.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
                || response.Contains("high demand", StringComparison.OrdinalIgnoreCase)
                || response.Contains("try again later", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTransientGeminiError(HttpStatusCode statusCode, string providerResponse)
        {
            if (statusCode == HttpStatusCode.ServiceUnavailable
                || statusCode == HttpStatusCode.TooManyRequests
                || statusCode == HttpStatusCode.InternalServerError
                || statusCode == HttpStatusCode.BadGateway
                || statusCode == HttpStatusCode.GatewayTimeout)
                return true;

            var response = providerResponse ?? string.Empty;
            return response.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
                || response.Contains("high demand", StringComparison.OrdinalIgnoreCase)
                || response.Contains("try again later", StringComparison.OrdinalIgnoreCase)
                || response.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeJsonResponse(string aiContent)
        {
            var trimmed = aiContent.Trim();
            if (!trimmed.StartsWith("```", StringComparison.Ordinal))
                return ExtractJsonObject(trimmed);

            var lines = trimmed.Split('\n');
            if (lines.Length < 3)
                return trimmed;

            var first = lines[0].Trim();
            var last = lines[^1].Trim();
            if (!first.StartsWith("```", StringComparison.Ordinal) || !last.Equals("```", StringComparison.Ordinal))
                return ExtractJsonObject(trimmed);

            var unfenced = string.Join('\n', lines.Skip(1).Take(lines.Length - 2)).Trim();
            return ExtractJsonObject(unfenced);
        }

        private static string ExtractJsonObject(string content)
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start < 0 || end <= start)
                return content;

            return content.Substring(start, end - start + 1);
        }
    }
}
