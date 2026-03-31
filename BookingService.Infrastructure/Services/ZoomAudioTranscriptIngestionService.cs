using BookingService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BookingService.Infrastructure.Services;

public class ZoomAudioTranscriptIngestionService : IZoomAudioTranscriptIngestionService
{
    private static readonly Regex TimestampRegex = new(
        @"^\s*(?<start>\d{2}:\d{2}:\d{2}\.\d{3})\s*-->\s*(?<end>\d{2}:\d{2}:\d{2}\.\d{3})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;
    private readonly IZoomService _zoomService;
    private readonly ILogger<ZoomAudioTranscriptIngestionService> _logger;
    private readonly string _aiTranscriptIngestPath;

    public ZoomAudioTranscriptIngestionService(
        HttpClient httpClient,
        IZoomService zoomService,
        IConfiguration configuration,
        ILogger<ZoomAudioTranscriptIngestionService> logger)
    {
        _httpClient = httpClient;
        _zoomService = zoomService;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);

        var aiBase = configuration["ServiceUrls:AIService"] ?? "http://localhost:5004";
        _httpClient.BaseAddress = new Uri(aiBase.TrimEnd('/') + "/");
        _aiTranscriptIngestPath = "api/transcripts/ingest/zoom-audio";
    }

    public async Task<Guid?> IngestZoomAudioTranscriptAsync(
        Guid bookingId,
        string meetingId,
        string transcriptDownloadUrl,
        string? zoomDownloadToken,
        string? sourceFileName,
        string? mimeType,
        long? fileSizeBytes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcriptDownloadUrl))
            return null;

        try
        {
            var oauthToken = await _zoomService.GetAccessToken(cancellationToken);
            var accessToken = FirstNonEmpty(zoomDownloadToken, oauthToken);
            var authorizedUrl = BuildZoomAuthorizedDownloadUrl(transcriptDownloadUrl.Trim(), accessToken);

            using var downloadRequest = new HttpRequestMessage(HttpMethod.Get, authorizedUrl);
            if (string.IsNullOrWhiteSpace(zoomDownloadToken))
                downloadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);

            using var downloadResponse = await _httpClient.SendAsync(
                downloadRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!downloadResponse.IsSuccessStatusCode)
            {
                var body = await downloadResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Zoom transcript download failed {Status} booking {BookingId}. Body={Body}",
                    downloadResponse.StatusCode,
                    bookingId,
                    body);
                return null;
            }

            var vttText = await ReadContentAsTextAsync(downloadResponse.Content, cancellationToken);
            var parsed = ParseVtt(vttText);
            if (string.IsNullOrWhiteSpace(parsed.RawText))
            {
                _logger.LogWarning("Zoom transcript download empty text for booking {BookingId}", bookingId);
                return null;
            }

            var payload = new ZoomAudioTranscriptIngestRequest
            {
                BookingId = bookingId,
                MeetingId = meetingId,
                Title = $"Zoom audio transcript {meetingId}",
                SourceFileName = sourceFileName,
                SourceUrl = transcriptDownloadUrl,
                MimeType = mimeType ?? "text/vtt",
                FileSizeBytes = fileSizeBytes,
                RawText = parsed.RawText,
                CleanText = parsed.CleanText,
                Segments = parsed.Segments
            };

            using var ingestRequest = new HttpRequestMessage(HttpMethod.Post, _aiTranscriptIngestPath)
            {
                Content = JsonContent.Create(payload)
            };
            using var ingestResponse = await _httpClient.SendAsync(ingestRequest, cancellationToken);
            var ingestBody = await ingestResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!ingestResponse.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "AI transcript ingest failed {Status} booking {BookingId}. Body={Body}",
                    ingestResponse.StatusCode,
                    bookingId,
                    ingestBody);
                return null;
            }

            var parsedResponse = JsonSerializer.Deserialize<CommonResponse<ZoomAudioTranscriptIngestResponse>>(
                ingestBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsedResponse?.Data?.TranscriptId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingest zoom audio transcript failed for booking {BookingId}", bookingId);
            return null;
        }
    }

    private static (string RawText, string CleanText, List<ZoomAudioTranscriptSegment> Segments) ParseVtt(string content)
    {
        var segments = new List<ZoomAudioTranscriptSegment>();
        if (string.IsNullOrWhiteSpace(content))
            return (string.Empty, string.Empty, segments);

        var lines = content.Replace("\r\n", "\n").Split('\n');
        var cueTextLines = new List<string>();
        double cueStart = 0;
        double cueEnd = 0;
        var inCue = false;

        void FlushCue()
        {
            if (!inCue || cueTextLines.Count == 0)
                return;

            var text = CollapseWhitespace(string.Join(" ", cueTextLines));
            if (!string.IsNullOrWhiteSpace(text))
            {
                segments.Add(new ZoomAudioTranscriptSegment
                {
                    StartSeconds = cueStart,
                    EndSeconds = cueEnd,
                    Text = text
                });
            }

            cueTextLines.Clear();
            inCue = false;
        }

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushCue();
                continue;
            }

            if (line.StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("NOTE", StringComparison.OrdinalIgnoreCase))
                continue;

            var match = TimestampRegex.Match(line);
            if (match.Success &&
                TryParseTimestamp(match.Groups["start"].Value, out cueStart) &&
                TryParseTimestamp(match.Groups["end"].Value, out cueEnd))
            {
                FlushCue();
                inCue = true;
                continue;
            }

            if (inCue)
                cueTextLines.Add(line);
        }

        FlushCue();

        if (segments.Count == 0)
        {
            var fallback = CollapseWhitespace(string.Join(" ", lines.Where(l =>
                !string.IsNullOrWhiteSpace(l) &&
                !l.Contains("-->", StringComparison.Ordinal) &&
                !l.Trim().StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))));
            return (fallback, fallback, segments);
        }

        var oneLine = CollapseWhitespace(string.Join(" ", segments.Select(s => s.Text)));
        return (oneLine, oneLine, segments);
    }

    private static bool TryParseTimestamp(string input, out double seconds)
    {
        seconds = 0;
        if (!TimeSpan.TryParseExact(input, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out var ts))
            return false;
        seconds = ts.TotalSeconds;
        return true;
    }

    private static string CollapseWhitespace(string value)
    {
        // Keep Vietnamese diacritics intact; only normalize spacing and Unicode composition.
        var normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormC);
        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }

    private static async Task<string> ReadContentAsTextAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0)
            return string.Empty;

        var encoding = ResolveEncoding(content.Headers.ContentType?.CharSet);
        var text = encoding.GetString(bytes);
        return text.Normalize(NormalizationForm.FormC);
    }

    private static Encoding ResolveEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
            return new UTF8Encoding(false, true);

        try
        {
            return Encoding.GetEncoding(charset.Trim('"'));
        }
        catch
        {
            return new UTF8Encoding(false, true);
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
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

    private sealed class ZoomAudioTranscriptIngestRequest
    {
        public Guid BookingId { get; set; }
        public string MeetingId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? SourceFileName { get; set; }
        public string? SourceUrl { get; set; }
        public string? MimeType { get; set; }
        public long? FileSizeBytes { get; set; }
        public string RawText { get; set; } = string.Empty;
        public string CleanText { get; set; } = string.Empty;
        public List<ZoomAudioTranscriptSegment> Segments { get; set; } = new();
    }

    private sealed class ZoomAudioTranscriptSegment
    {
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    private sealed class CommonResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private sealed class ZoomAudioTranscriptIngestResponse
    {
        public Guid TranscriptId { get; set; }
    }
}
