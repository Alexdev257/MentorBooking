using BookingService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace BookingService.Infrastructure.Services;

public class ZoomRecordingAiUploadService : IZoomRecordingAiUploadService
{
    private static readonly ConcurrentDictionary<string, DateTime> RecentUploadKeys = new();
    private static readonly TimeSpan UploadDedupWindow = TimeSpan.FromHours(6);

    private readonly HttpClient _httpClient;
    private readonly IZoomService _zoomService;
    private readonly ILogger<ZoomRecordingAiUploadService> _logger;

    public ZoomRecordingAiUploadService(
        HttpClient httpClient,
        IZoomService zoomService,
        IConfiguration configuration,
        ILogger<ZoomRecordingAiUploadService> logger)
    {
        _httpClient = httpClient;
        _zoomService = zoomService;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromMinutes(15);

        var aiBase = configuration["ServiceUrls:AIService"] ?? "http://localhost:5004";
        _httpClient.BaseAddress = new Uri(aiBase.TrimEnd('/') + "/");
    }

    public async Task<(bool IsSuccess, string? TranscriptId)> UploadRecordingToAiAsync(
        Guid bookingId,
        string meetingId,
        string recordingDownloadUrl,
        string? zoomDownloadToken,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recordingDownloadUrl))
            return (false, null);

        var uploadKey = BuildUploadKey(bookingId, meetingId, recordingDownloadUrl);
        if (IsDuplicateRecently(uploadKey))
        {
            _logger.LogInformation(
                "Skip duplicate AI upload for booking {BookingId}, meeting {MeetingId}, key {Key}",
                bookingId,
                meetingId,
                uploadKey);
            return (true, null);
        }

        try
        {
            CleanupExpiredKeys();
            var oauthToken = await _zoomService.GetAccessToken(cancellationToken);
            var accessToken = string.IsNullOrWhiteSpace(zoomDownloadToken) ? oauthToken : zoomDownloadToken;
            var authorizedUrl = BuildZoomAuthorizedDownloadUrl(recordingDownloadUrl.Trim(), accessToken);

            _logger.LogInformation(
                "Preparing AI upload for booking {BookingId}, meeting {MeetingId}. Endpoint={Endpoint}, FileName={FileName}, ContentType={ContentType}, Source={Source}",
                bookingId,
                meetingId,
                "api/transcripts/upload",
                fileName,
                contentType,
                RedactUrl(recordingDownloadUrl));

            using var downloadRequest = new HttpRequestMessage(HttpMethod.Get, authorizedUrl);
            if (string.IsNullOrWhiteSpace(zoomDownloadToken))
            {
                downloadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);
            }

            using var downloadResponse = await _httpClient.SendAsync(
                downloadRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!downloadResponse.IsSuccessStatusCode)
            {
                var body = await downloadResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Zoom mp4 download failed {Status} for booking {BookingId}: {Body}",
                    downloadResponse.StatusCode,
                    bookingId,
                    body);
                return (false, null);
            }

            await using var zoomStream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken);
            var sourceSize = downloadResponse.Content.Headers.ContentLength;
            using var form = new MultipartFormDataContent();
            var streamContent = new StreamContent(zoomStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(streamContent, "file", fileName);
            form.Add(new StringContent($"Zoom recording {meetingId}"), "title");
            form.Add(new StringContent("3"), "sourceType"); // RecordVideo

            _logger.LogInformation(
                "Sending multipart to AI. booking={BookingId}, meeting={MeetingId}, fields=[file,title,sourceType], title={Title}, sourceLength={SourceLength}",
                bookingId,
                meetingId,
                $"Zoom recording {meetingId}",
                sourceSize?.ToString() ?? "unknown");

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/transcripts/upload")
            {
                Content = form
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "AI upload mp4 failed {Status} for booking {BookingId}: {Body}",
                    response.StatusCode,
                    bookingId,
                    responseBody);
                return (false, null);
            }

            _logger.LogInformation(
                "Uploaded Zoom mp4 to AIService upload endpoint for booking {BookingId}, meeting {MeetingId}. Response={Response}",
                bookingId,
                meetingId,
                TrimForLog(responseBody, 400));
            RecentUploadKeys[uploadKey] = DateTime.UtcNow;

            string? transcriptId = null;
            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("id", out var idEl))
                {
                    transcriptId = idEl.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse AI upload response to extract transcript ID for booking {BookingId}", bookingId);
            }

            return (true, transcriptId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload Zoom mp4 to AIService failed for booking {BookingId}", bookingId);
            return (false, null);
        }
    }

    private static string BuildZoomAuthorizedDownloadUrl(string baseUrl, string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(accessToken))
            return baseUrl;
        if (baseUrl.Contains("access_token=", StringComparison.OrdinalIgnoreCase))
            return baseUrl;

        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}access_token={Uri.EscapeDataString(accessToken)}";
    }

    private static string BuildUploadKey(Guid bookingId, string meetingId, string recordingDownloadUrl)
    {
        var raw = $"{bookingId:N}|{meetingId}|{recordingDownloadUrl}".ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..24];
    }

    private static bool IsDuplicateRecently(string key) =>
        RecentUploadKeys.TryGetValue(key, out var at) && DateTime.UtcNow - at < UploadDedupWindow;

    private static void CleanupExpiredKeys()
    {
        var now = DateTime.UtcNow;
        foreach (var pair in RecentUploadKeys)
        {
            if (now - pair.Value >= UploadDedupWindow)
                RecentUploadKeys.TryRemove(pair.Key, out _);
        }
    }

    private static string RedactUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;
        var q = url.IndexOf('?');
        return q >= 0 ? $"{url[..q]}?*" : url;
    }

    private static string TrimForLog(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..max] + "...";
    }
}
