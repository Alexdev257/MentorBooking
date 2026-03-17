using AIService.Application.DTOs.Summary;
using AIService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI;
using System.Text.Json;

namespace AIService.Infrastructure.Services;

public class GeminiSummaryService : ISummaryService
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiSummaryService> _logger;

    private const string SystemPrompt = """
        You are a meeting assistant. Analyze the following transcript and return ONLY a valid JSON object (no markdown, no code blocks) with this exact structure:
        {
          "summary": "A concise 2-3 paragraph summary of the meeting",
          "keyPoints": ["key point 1", "key point 2", "key point 3"],
          "topics": ["topic 1", "topic 2"],
          "sentiment": "positive or neutral or negative"
        }
        """;

    public GeminiSummaryService(IConfiguration configuration, ILogger<GeminiSummaryService> logger)
    {
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        _model = configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        _logger = logger;
    }

    public async Task<SummaryResponseDto?> SummarizeAsync(string transcriptText, CancellationToken cancellationToken = default)
    {
        try
        {
            var googleAI = new GoogleAI(_apiKey);
            var model = googleAI.GenerativeModel(_model);

            var prompt = $"{SystemPrompt}\n\nTranscript:\n{transcriptText}";
            var response = await model.GenerateContent(prompt);
            var rawText = response.Text;

            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("Gemini returned empty response.");
                return null;
            }

            var cleaned = CleanJsonResponse(rawText);
            var parsed = JsonSerializer.Deserialize<GeminiSummaryResult>(cleaned, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null) return null;

            return new SummaryResponseDto
            {
                Summary = parsed.Summary ?? string.Empty,
                KeyPoints = parsed.KeyPoints ?? new List<string>(),
                Topics = parsed.Topics ?? new List<string>(),
                Sentiment = parsed.Sentiment ?? "neutral",
                Model = _model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary with Gemini.");
            return null;
        }
    }

    private static string CleanJsonResponse(string text)
    {
        // Remove markdown code blocks if present
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end >= 0)
                return trimmed.Substring(start, end - start + 1);
        }
        return trimmed;
    }

    private class GeminiSummaryResult
    {
        public string? Summary { get; set; }
        public List<string>? KeyPoints { get; set; }
        public List<string>? Topics { get; set; }
        public string? Sentiment { get; set; }
    }
}
