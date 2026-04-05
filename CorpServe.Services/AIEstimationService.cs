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
            var prompt = BuildPrompt(estimateDTO);
            var aiResponse = await ExecutePromptAsync(
                prompt,
                "You are a practical estimation assistant for all service categories, not software-only. Return only valid JSON.");
            if (aiResponse.IsFailure)
                return aiResponse.Errors.ToList();

            try
            {
                using var estimateJson = JsonDocument.Parse(aiResponse.Value);
                var root = estimateJson.RootElement;

                var understood = true;
                if (root.TryGetProperty("understood", out var understoodProperty))
                {
                    if (understoodProperty.ValueKind == JsonValueKind.True || understoodProperty.ValueKind == JsonValueKind.False)
                        understood = understoodProperty.GetBoolean();
                }

                if (!understood)
                {
                    var clarificationMessage = TryGetString(root, "clarificationMessage")
                        ?? TryGetString(root, "reason")
                        ?? "Your request details are unclear. Please provide a clear service title and a detailed description of scope, deliverables, and context.";
                    var missingDetails = TryGetStringArray(root, "missingDetails");
                    var userFriendlyMessage = BuildUserFriendlyClarificationMessage(clarificationMessage, missingDetails);

                    return Error.Validation("AI.UnclearRequest", userFriendlyMessage);
                }

                if (!TryGetDecimal(root, "estimatedCost", out var estimatedCost)
                    || !TryGetInt(root, "estimatedDays", out var estimatedDays)
                    || !TryGetInt(root, "confidence", out var confidence))
                {
                    return Error.Failure("AI.ParseFailed", "AI response is missing required estimation fields.");
                }

                if (estimatedDays <= 0)
                    return Error.Failure("AI.ParseFailed", "AI returned invalid estimatedDays.");

                if (estimatedCost <= 0)
                    return Error.Failure("AI.ParseFailed", "AI returned invalid estimatedCost.");

                var hasRealisticBudgetMin = TryGetDecimal(root, "realisticBudgetMin", out var realisticBudgetMin);
                var hasRealisticBudgetMax = TryGetDecimal(root, "realisticBudgetMax", out var realisticBudgetMax);
                if (estimatedCost < estimateDTO.BudgetMin || estimatedCost > estimateDTO.BudgetMax)
                    return Error.Validation(
                        "AI.UnrealisticBudget",
                        BuildBudgetUnrealisticMessage(
                            estimatedCost,
                            hasRealisticBudgetMin ? realisticBudgetMin : null,
                            hasRealisticBudgetMax ? realisticBudgetMax : null));

                var daysUntilDeadline = (int)Math.Ceiling((estimateDTO.ExpectedDeadline.ToUniversalTime() - DateTime.UtcNow).TotalDays);
                if (daysUntilDeadline < 1)
                    return Error.Validation("AI.InvalidDeadline", "Expected deadline must allow enough time for estimation.");

                if (estimatedDays > daysUntilDeadline)
                    return Error.Validation("AI.UnrealisticTimeline", BuildTimelineUnrealisticMessage(estimatedDays));

                if (confidence < 0 || confidence > 100)
                    return Error.Failure("AI.ParseFailed", "AI returned invalid confidence range.");

                var result = new AIEstimationDTO
                {
                    EstimatedCost = estimatedCost,
                    EstimatedTime = DateTime.UtcNow.AddDays(estimatedDays),
                    Confidence = confidence
                };

                return result;
            }
            catch
            {
                return Error.Failure("AI.ParseFailed", "Failed to parse AI estimation response.");
            }
        }

        public async Task<Result> ReviewRequestClarityAsync(GenerateRequestEstimateDTO reviewDTO)
        {
            var prompt = BuildReviewPrompt(reviewDTO);
            var aiResponse = await ExecutePromptAsync(
                prompt,
                "You are a practical request-clarity reviewer. Return only valid JSON.");
            if (aiResponse.IsFailure)
                return Result.Fail(aiResponse.Errors.ToList());

            try
            {
                using var reviewJson = JsonDocument.Parse(aiResponse.Value);
                var root = reviewJson.RootElement;

                var understood = true;
                if (root.TryGetProperty("understood", out var understoodProperty))
                {
                    if (understoodProperty.ValueKind == JsonValueKind.True || understoodProperty.ValueKind == JsonValueKind.False)
                        understood = understoodProperty.GetBoolean();
                }

                if (!understood)
                {
                    var clarificationMessage = TryGetString(root, "clarificationMessage")
                        ?? TryGetString(root, "reason")
                        ?? "Your request details are unclear. Please provide a clear service title and a detailed description of scope, deliverables, and context.";
                    var missingDetails = TryGetStringArray(root, "missingDetails");

                    var userFriendlyMessage = BuildUserFriendlyClarificationMessage(clarificationMessage, missingDetails);

                    return Result.Fail(Error.Validation("AI.UnclearRequest", userFriendlyMessage));
                }

                if (TryGetBool(root, "budgetRealistic", out var budgetRealistic) && !budgetRealistic)
                {
                    TryGetDecimal(root, "realisticBudgetMin", out var realisticBudgetMin);
                    TryGetDecimal(root, "realisticBudgetMax", out var realisticBudgetMax);
                    var budgetFeedback = TryGetString(root, "budgetFeedback");

                    return Result.Fail(Error.Validation(
                        "AI.UnrealisticBudget",
                        string.IsNullOrWhiteSpace(budgetFeedback)
                            ? BuildBudgetUnrealisticMessage(null, realisticBudgetMin, realisticBudgetMax)
                            : budgetFeedback));
                }

                return Result.Ok();
            }
            catch
            {
                return Result.Fail(Error.Failure("AI.ParseFailed", "Failed to parse AI review response."));
            }
        }

        private static string BuildPrompt(GenerateRequestEstimateDTO estimateDTO)
        {
            return $@"You are a practical service-request estimator for all types of services (not software-only).

First, decide whether the request has enough usable details for a reasonable estimate.
Do not reject for minor ambiguity. Reject only when essential details are missing, the content is mostly nonsense, or the request is not actionable.
Short descriptions are acceptable if the core need is understandable.

Input:
- Title: {estimateDTO.Title}
- Description: {estimateDTO.Description}
- CategoryId: {estimateDTO.CategoryId}
- BudgetMin: {estimateDTO.BudgetMin}
- BudgetMax: {estimateDTO.BudgetMax}
- ExpectedDeadline: {estimateDTO.ExpectedDeadline:O}

Return ONLY valid JSON with this exact schema:
{{
  ""understood"": boolean,
  ""clarificationMessage"": string | null,
  ""missingDetails"": string[] | null,
  ""estimatedCost"": number | null,
  ""estimatedDays"": number | null,
  ""confidence"": number | null
}}

Rules:
- If the request is clearly not actionable (major missing details / nonsense / contradictory core info):
  - set understood=false
  - set clarificationMessage as one short, simple sentence for non-technical users (no internal IDs, no long paragraphs)
  - set missingDetails to at most 1 item from: [""serviceScope"", ""tasks"", ""deliverables"", ""purpose"", ""constraints"", ""timelineContext"", ""budgetContext""]
  - set estimatedCost/estimatedDays/confidence = null
- If the request is actionable (even if not perfect):
  - set understood=true
  - clarificationMessage=null
  - missingDetails=null
  - accept concise requests and infer typical service assumptions when needed
  - do NOT ask for optional operational details (e.g., exact square footage, exact working hours, exact frequency) when the core request is already understandable
  - treat all budget and cost values as Egyptian Pounds (EGP)
  - First determine realisticBudgetMin and realisticBudgetMax from scope ONLY (independent from the user budget)
  - Then set estimatedCost inside that realistic range (prefer midpoint unless scope justifies otherwise)
  - keep estimates stable and grounded; avoid large shifts caused only by changing the user budget
  - if details are partially clear, provide a conservative estimate and lower confidence
  - NEVER anchor the estimate to an unrealistic user budget
  - realisticBudgetMin must be <= estimatedCost <= realisticBudgetMax
  - estimatedDays must be positive and not exceed days until ExpectedDeadline
  - confidence must be from 0 to 100.";
        }

        private static string BuildReviewPrompt(GenerateRequestEstimateDTO reviewDTO)
        {
            return $@"You are a practical service-request clarity reviewer for all service categories (not software-only).

Your job is to review whether this request is clear enough for vendors to understand and price.
Do NOT estimate cost, duration, or confidence.
Be lenient: concise requests are valid when the core need is clear.

Input:
- Title: {reviewDTO.Title}
- Description: {reviewDTO.Description}
- CategoryId: {reviewDTO.CategoryId}
- BudgetMin (EGP): {reviewDTO.BudgetMin}
- BudgetMax (EGP): {reviewDTO.BudgetMax}
- ExpectedDeadline: {reviewDTO.ExpectedDeadline:O}

Return ONLY valid JSON with this exact schema:
{{
  ""understood"": boolean,
  ""clarificationMessage"": string | null,
  ""missingDetails"": string[] | null
}}

Rules:
- Only block the request when major details are missing or the content is not actionable for vendors.
- Do not block for minor ambiguity if vendors can still understand the core need.
- Do not block the request just because optional operational details are missing (e.g., exact size, exact schedule, exact frequency) when the core scope is clear.
- If the request should be blocked:
  - set understood=false
  - set clarificationMessage as one short, simple sentence for non-technical users (no internal IDs/codes)
  - set missingDetails to at most 1 item from: [""serviceScope"", ""tasks"", ""deliverables"", ""purpose"", ""constraints"", ""timelineContext"", ""budgetContext""]
- If the request is clear enough to proceed:
  - set understood=true
  - clarificationMessage=null
  - missingDetails=null";
        }

        private async Task<Result<string>> ExecutePromptAsync(string prompt, string systemMessage)
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
                        temperature = 0.1
                    }
                }
                : new
                {
                    model,
                    messages = new object[]
                    {
                        new { role = "system", content = systemMessage },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.1,
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
                    $"Failed to process AI request. StatusCode: {(int)statusCode}. ProviderResponse: {providerMessage}");
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

                return NormalizeJsonResponse(content);
            }
            catch
            {
                return Error.Failure("AI.ParseFailed", "Failed to parse AI provider response.");
            }
        }

        private static string? TryGetString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                return null;

            return property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : property.ToString();
        }

        private static bool TryGetDecimal(JsonElement root, string propertyName, out decimal value)
        {
            value = 0m;
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                return false;

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out value))
                return true;

            if (property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), out value))
                return true;

            return false;
        }

        private static bool TryGetBool(JsonElement root, string propertyName, out bool value)
        {
            value = false;
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                return false;

            if (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
            {
                value = property.GetBoolean();
                return true;
            }

            if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out value))
                return true;

            return false;
        }

        private static bool TryGetInt(JsonElement root, string propertyName, out int value)
        {
            value = 0;
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                return false;

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value))
                return true;

            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value))
                return true;

            return false;
        }

        private static List<string> TryGetStringArray(JsonElement root, string propertyName)
        {
            var result = new List<string>();
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in property.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        result.Add(value.Trim());
                }
            }

            return result;
        }

        private static string BuildUserFriendlyClarificationMessage(string? rawMessage, List<string> missingDetails)
        {
            var guidanceParts = new List<string>();
            if (missingDetails.Contains("serviceScope", StringComparer.OrdinalIgnoreCase))
                guidanceParts.Add("what service you need");
            if (missingDetails.Contains("tasks", StringComparer.OrdinalIgnoreCase))
                guidanceParts.Add("the key tasks");
            if (missingDetails.Contains("deliverables", StringComparer.OrdinalIgnoreCase))
                guidanceParts.Add("expected deliverables");
            if (missingDetails.Contains("purpose", StringComparer.OrdinalIgnoreCase))
                guidanceParts.Add("the business purpose");
            if (missingDetails.Contains("constraints", StringComparer.OrdinalIgnoreCase))
                guidanceParts.Add("important constraints/requirements");
            if (missingDetails.Contains("timelineContext", StringComparer.OrdinalIgnoreCase))
                guidanceParts.Add("timeline details");
            if (missingDetails.Contains("budgetContext", StringComparer.OrdinalIgnoreCase))
                guidanceParts.Add("realistic budget context in EGP");

            // Keep review messages short and easy for clients.
            var topGuidance = guidanceParts
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(1)
                .ToList();

            if (topGuidance.Count == 0)
                return "Please add a bit more detail so vendors can understand and price your request.";

            var cleaned = RemoveInternalCategoryIds((rawMessage ?? string.Empty).Trim());
            if (cleaned.Length > 140)
                cleaned = string.Empty;

            if (string.IsNullOrWhiteSpace(cleaned))
                return $"Please add: {topGuidance[0]}.";

            return $"{cleaned} Please add: {topGuidance[0]}.";
        }

        private static string BuildBudgetUnrealisticMessage(decimal? estimatedCost, decimal? realisticBudgetMin, decimal? realisticBudgetMax)
        {
            if (realisticBudgetMin.HasValue && realisticBudgetMax.HasValue)
                return $"The provided budget is not realistic for this scope. A practical range is around {realisticBudgetMin.Value:0.##} to {realisticBudgetMax.Value:0.##} EGP.";

            if (estimatedCost.HasValue)
                return $"The provided budget is not realistic for this scope. The expected cost is around {estimatedCost.Value:0.##} EGP.";

            return "The provided budget is not realistic for this scope. Please increase your budget to match market pricing.";
        }

        private static string BuildTimelineUnrealisticMessage(int estimatedDays)
        {
            return $"The provided deadline is not realistic for this scope. A practical timeline is حوالي {estimatedDays} يوم.";
        }

        private static string RemoveInternalCategoryIds(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Remove raw category IDs like C-005 to avoid technical leakage in UX messages.
            var withoutId = System.Text.RegularExpressions.Regex.Replace(
                input,
                @"['""]?\bC-\d{3,}\b['""]?",
                "the selected category",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

            // Normalize extra spaces left by replacements.
            return System.Text.RegularExpressions.Regex.Replace(withoutId, @"\s{2,}", " ").Trim();
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
