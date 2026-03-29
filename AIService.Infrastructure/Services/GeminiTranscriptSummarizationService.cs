using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AIService.Application.Configuration;
using AIService.Application.DTOs.Transcripts;
using AIService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIService.Infrastructure.Services;

public class GeminiTranscriptSummarizationService : ITranscriptSummarizationService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiTranscriptSummarizationService> _logger;

    public GeminiTranscriptSummarizationService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiTranscriptSummarizationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<TranscriptSummaryDto?> SummarizeAsync(string transcriptText, string? title, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return null;

        var text = transcriptText.Trim();
        if (text.Length == 0)
            return null;

        var max = Math.Max(10_000, _options.MaxInputCharacters);
        if (text.Length > max)
            text = text[..max] + "\n\n[Transcript truncated for summarization.]";

        var userPrompt =
            "You are summarizing a mentor session or meeting transcript (plain text from speech-to-text; speakers are not labeled).\n" +
            "Produce a detailed, accurate summary in Vietnamese unless the transcript is clearly in another language (then match that language).\n" +
            "Include: main themes, concrete advice, decisions, follow-ups, and notable terms.\n\n" +
            (string.IsNullOrWhiteSpace(title) ? "" : $"Session title: {title}\n\n") +
            "Transcript:\n" +
            text;

        var requestUri = $"v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);

        var bodyJson = BuildRequestBodyJson(userPrompt);
        request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Gemini request timed out.");
            throw new InvalidOperationException("Gemini request timed out (limit: 3 minutes). The transcript may be too long.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Gemini request failed (network).");
            throw new InvalidOperationException($"Cannot connect to Gemini API: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Gemini request failed (network).");
            throw new InvalidOperationException($"Gemini request failed: {ex.Message}", ex);
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini API error {Status}: {Body}", (int)response.StatusCode, raw.Length > 500 ? raw[..500] : raw);

            var errorDetail = ExtractGeminiErrorMessage(raw) ?? $"HTTP {(int)response.StatusCode}";
            throw new InvalidOperationException($"Gemini API error: {errorDetail}");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
            {
                var reason = ExtractBlockReason(raw);
                _logger.LogWarning("Gemini response has no candidates: {Body}", raw.Length > 400 ? raw[..400] : raw);
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(reason)
                        ? "Gemini returned no candidates (empty response)."
                        : $"Gemini blocked the request: {reason}");
            }

            var first = candidates[0];
            if (!first.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                var finishReason = first.TryGetProperty("finishReason", out var fr) ? fr.GetString() : null;
                throw new InvalidOperationException(
                    $"Gemini candidate has no content (finishReason: {finishReason ?? "unknown"}).");
            }

            if (!parts[0].TryGetProperty("text", out var textEl))
                throw new InvalidOperationException("Gemini response part has no text field.");
            var textPart = textEl.GetString();
            if (string.IsNullOrWhiteSpace(textPart))
                throw new InvalidOperationException("Gemini returned an empty text response.");

            using var summaryDoc = JsonDocument.Parse(textPart);
            var root = summaryDoc.RootElement;

            var summary = root.GetProperty("summary").GetString() ?? "";
            var keyPoints = ReadStringArray(root, "keyPoints");
            var topics = ReadStringArray(root, "topics");
            var sentimentJson = "{}";
            if (root.TryGetProperty("sentiment", out var sentimentEl))
                sentimentJson = sentimentEl.GetRawText();

            return new TranscriptSummaryDto
            {
                Summary = summary.Trim(),
                KeyPoints = keyPoints,
                Topics = topics,
                SentimentJson = string.IsNullOrWhiteSpace(sentimentJson) ? "{}" : sentimentJson,
                Model = _options.Model,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }
        catch (InvalidOperationException) { throw; }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini JSON response. Raw: {Raw}", raw.Length > 300 ? raw[..300] : raw);
            throw new InvalidOperationException($"Failed to parse Gemini response as JSON: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini summarization response.");
            throw new InvalidOperationException($"Unexpected error parsing Gemini response: {ex.Message}", ex);
        }
    }

    private static List<string> ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return new List<string>();
        var list = new List<string>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    list.Add(s.Trim());
            }
        }
        return list;
    }

    private static string? ExtractGeminiErrorMessage(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch { }
        return raw.Length > 200 ? raw[..200] : raw;
    }

    private static string? ExtractBlockReason(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("promptFeedback", out var fb) &&
                fb.TryGetProperty("blockReason", out var br))
                return br.GetString();
        }
        catch { }
        return null;
    }

    private static string BuildRequestBodyJson(string userPrompt)
    {
        var responseSchema = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["summary"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Detailed summary of the transcript."
                },
                ["keyPoints"] = new JsonObject
                {
                    ["type"] = "ARRAY",
                    ["items"] = new JsonObject { ["type"] = "STRING" }
                },
                ["topics"] = new JsonObject
                {
                    ["type"] = "ARRAY",
                    ["items"] = new JsonObject { ["type"] = "STRING" }
                },
                ["sentiment"] = new JsonObject
                {
                    ["type"] = "OBJECT",
                    ["properties"] = new JsonObject
                    {
                        ["overall"] = new JsonObject { ["type"] = "STRING" },
                        ["notes"] = new JsonObject { ["type"] = "STRING" }
                    }
                }
            },
            ["required"] = new JsonArray("summary", "keyPoints", "topics", "sentiment")
        };

        var root = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["text"] = userPrompt }
                    }
                }
            },
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = 0.35,
                ["responseMimeType"] = "application/json",
                ["responseSchema"] = responseSchema
            }
        };

        return root.ToJsonString();
    }
}
