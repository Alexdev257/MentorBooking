using BookingService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookingService.Infrastructure.Services;

public class ZoomVideoTranscriptionService : IZoomVideoTranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZoomVideoTranscriptionService> _logger;
    private const string UploadFromUrlPath = "api/transcripts/upload-from-url";

    public ZoomVideoTranscriptionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ZoomVideoTranscriptionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);

        var aiBase = configuration["ServiceUrls:AIService"] ?? "http://localhost:5004";
        _httpClient.BaseAddress = new Uri(aiBase.TrimEnd('/') + "/");
    }

    public async Task<Guid?> TriggerTranscriptionAsync(
        Guid bookingId,
        string videoUrl,
        string title,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            return null;

        try
        {
            var payload = new
            {
                url = videoUrl,
                title = title,
                sourceType = 3, // RecordVideo
                contentType = contentType ?? "video/mp4"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, UploadFromUrlPath)
            {
                Content = JsonContent.Create(payload)
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "AI upload-from-url failed {Status} for booking {BookingId}. Body={Body}",
                    response.StatusCode, bookingId, body);
                return null;
            }

            var parsed = JsonSerializer.Deserialize<CommonResponse<UploadResponse>>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed?.Data?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TriggerTranscriptionAsync failed for booking {BookingId}", bookingId);
            return null;
        }
    }

    private sealed class CommonResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
    }

    private sealed class UploadResponse
    {
        public Guid Id { get; set; }
    }
}
