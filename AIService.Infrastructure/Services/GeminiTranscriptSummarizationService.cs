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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini request failed (network).");
            return null;
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini API error {Status}: {Body}", (int)response.StatusCode, raw.Length > 500 ? raw[..500] : raw);
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning("Gemini response has no candidates: {Body}", raw.Length > 400 ? raw[..400] : raw);
                return null;
            }

            var first = candidates[0];
            if (!first.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
                return null;

            if (!parts[0].TryGetProperty("text", out var textEl))
                return null;
            var textPart = textEl.GetString();
            if (string.IsNullOrWhiteSpace(textPart))
                return null;

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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini summarization response.");
            return null;
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
