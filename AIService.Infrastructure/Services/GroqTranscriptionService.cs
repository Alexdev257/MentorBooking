using System.Net.Http.Headers;
using System.Text.Json;
using AIService.Application.DTOs.Transcripts;
using AIService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIService.Infrastructure.Services;

public class GroqTranscriptionService : ITranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqTranscriptionService> _logger;

    public GroqTranscriptionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GroqTranscriptionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TranscriptionResult> TranscribeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found for transcription.", filePath);

        var apiKey = _configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException(
                "Groq:ApiKey is not configured. Add 'Groq:ApiKey' to appsettings or environment variables.");

        var model = _configuration["Groq:Model"] ?? "whisper-large-v3-turbo";
        var fileName = Path.GetFileName(filePath);

        _logger.LogInformation("Sending {FileName} to Groq Whisper API (model: {Model})", fileName, model);

        await using var fileStream = File.OpenRead(filePath);

        using var formContent = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ResolveMimeType(fileName));
        formContent.Add(fileContent, "file", fileName);
        formContent.Add(new StringContent(model), "model");
        formContent.Add(new StringContent("verbose_json"), "response_format");

        using var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/audio/transcriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = formContent;

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Groq API error {StatusCode}: {Body}", (int)response.StatusCode, responseBody);
            throw new InvalidOperationException($"Groq Whisper API returned {(int)response.StatusCode}: {responseBody}");
        }

        var groqResponse = JsonSerializer.Deserialize<GroqTranscriptionResponse>(responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (groqResponse == null)
            throw new InvalidOperationException("Groq API returned an empty or invalid response.");

        var result = new TranscriptionResult
        {
            FullText = groqResponse.Text ?? string.Empty
        };

        if (groqResponse.Segments != null)
        {
            foreach (var seg in groqResponse.Segments)
            {
                if (string.IsNullOrWhiteSpace(seg.Text))
                    continue;

                result.Segments.Add(new TranscriptSegmentDto
                {
                    StartSeconds = seg.Start,
                    EndSeconds = seg.End,
                    Text = seg.Text.Trim()
                });
            }
        }

        _logger.LogInformation(
            "Groq transcription completed: {SegmentCount} segments, {CharCount} characters",
            result.Segments.Count, result.FullText.Length);

        return result;
    }

    private static string ResolveMimeType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".mp4"  => "video/mp4",
            ".webm" => "video/webm",
            ".mov"  => "video/quicktime",
            ".avi"  => "video/x-msvideo",
            ".mp3"  => "audio/mpeg",
            ".wav"  => "audio/wav",
            ".m4a"  => "audio/mp4",
            ".ogg"  => "audio/ogg",
            ".flac" => "audio/flac",
            ".mpeg" => "audio/mpeg",
            ".mpga" => "audio/mpeg",
            _       => "application/octet-stream"
        };
    }

    // --- Internal DTOs for Groq response deserialization ---

    private sealed class GroqTranscriptionResponse
    {
        public string? Text { get; set; }
        public List<GroqSegment>? Segments { get; set; }
    }

    private sealed class GroqSegment
    {
        public double Start { get; set; }
        public double End { get; set; }
        public string? Text { get; set; }
    }
}
