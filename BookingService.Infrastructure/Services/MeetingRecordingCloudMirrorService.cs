using BookingService.Application.Interfaces.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace BookingService.Infrastructure.Services;

public class MeetingRecordingCloudMirrorService : IMeetingRecordingCloudMirrorService
{
    private readonly HttpClient _httpClient;
    private readonly IZoomService _zoomService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MeetingRecordingCloudMirrorService> _logger;
    private readonly StorageClient? _storageClient;
    private readonly string _bucketName;

    public MeetingRecordingCloudMirrorService(
        HttpClient httpClient,
        IZoomService zoomService,
        IConfiguration configuration,
        ILogger<MeetingRecordingCloudMirrorService> logger)
    {
        _httpClient = httpClient;
        _zoomService = zoomService;
        _configuration = configuration;
        _logger = logger;
        _bucketName = configuration["Firebase:BucketName"] ?? string.Empty;
        _httpClient.Timeout = TimeSpan.FromMinutes(30);

        var credential = TryLoadGoogleCredential(configuration);
        if (credential == null)
        {
            _logger.LogWarning(
                "MeetingRecordingCloudMirror: Firebase chưa cấu hình (Firebase__BucketName + credential). Chỉ lưu URL Zoom.");
            _storageClient = null;
            return;
        }

        _storageClient = StorageClient.Create(credential);
    }

    /// <inheritdoc />
    public async Task<string?> TryMirrorToFirebaseAsync(
        Guid bookingId,
        string zoomMeetingNumericId,
        string zoomDownloadUrl,
        string storageFileLabel,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zoomDownloadUrl))
            return null;

        if (_storageClient == null || string.IsNullOrWhiteSpace(_bucketName))
            return null;

        var safeMeeting = SanitizePathSegment(zoomMeetingNumericId);
        var safeLabel = SanitizePathSegment(storageFileLabel);
        var ext = NormalizeExtension(extension);
        var objectName =
            $"meetings/{bookingId:N}/zoom-{safeMeeting}/{safeLabel}-{Guid.NewGuid():N}{ext}";

        try
        {
            var token = await _zoomService.GetAccessToken(cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, zoomDownloadUrl.Trim());
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Zoom recording download failed {Status} for booking {BookingId}: {Body}",
                    response.StatusCode,
                    bookingId,
                    body);
                return null;
            }

            await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var uploaded = await _storageClient.UploadObjectAsync(
                _bucketName,
                objectName,
                contentType,
                networkStream,
                cancellationToken: cancellationToken);

            try
            {
                uploaded.Acl = new List<ObjectAccessControl>
                {
                    new ObjectAccessControl { Entity = "allUsers", Role = "READER" }
                };
                await _storageClient.UpdateObjectAsync(uploaded, cancellationToken: cancellationToken);
            }
            catch (Exception aclEx)
            {
                _logger.LogInformation(
                    aclEx,
                    "Firebase object ACL skipped (uniform bucket access). Use bucket IAM or signed URLs if needed.");
            }

            var url =
                $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";

            _logger.LogInformation(
                "Mirrored Zoom file to Firebase for booking {BookingId}: {ObjectName}",
                bookingId,
                objectName);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mirror Zoom → Firebase failed for booking {BookingId}", bookingId);
            return null;
        }
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";
        var sb = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_')
                sb.Append(c);
        }
        return sb.Length > 0 ? sb.ToString() : "unknown";
    }

    private static string NormalizeExtension(string extension)
    {
        var e = (extension ?? ".bin").Trim().ToLowerInvariant();
        if (!e.StartsWith('.'))
            e = "." + e;
        return e;
    }

    /// <summary>Cùng thứ tự ưu tiên credential như AuthService FirebaseService (JSON, Base64, path, GOOGLE_APPLICATION_CREDENTIALS).</summary>
    private static GoogleCredential? TryLoadGoogleCredential(IConfiguration configuration)
    {
        var json = configuration["Firebase:CredentialJson"];
        if (!string.IsNullOrWhiteSpace(json))
            return GoogleCredential.FromJson(json.Trim());

        var b64 = configuration["Firebase:CredentialJsonBase64"];
        if (!string.IsNullOrWhiteSpace(b64))
        {
            var bytes = Convert.FromBase64String(b64.Trim());
            return GoogleCredential.FromJson(Encoding.UTF8.GetString(bytes));
        }

        var credentialPath = configuration["Firebase:CredentialPath"];
        if (!string.IsNullOrWhiteSpace(credentialPath) && File.Exists(credentialPath))
            return GoogleCredential.FromFile(credentialPath);

        var gac = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (!string.IsNullOrWhiteSpace(gac) && File.Exists(gac))
            return GoogleCredential.FromFile(gac);

        return null;
    }
}
