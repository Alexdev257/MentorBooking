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

        var credential = TryLoadGoogleCredential();
        if (credential == null)
        {
            _logger.LogWarning(
                "MeetingRecordingCloudMirror: không load được Firebase credential (kiểm tra Firebase__CredentialJson hoặc file tại Firebase__CredentialPath / /etc/secrets). BucketName empty={BucketEmpty}. Chỉ lưu URL Zoom.",
                string.IsNullOrWhiteSpace(_bucketName));
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
    private GoogleCredential? TryLoadGoogleCredential()
    {
        var json = _configuration["Firebase:CredentialJson"];
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                return GoogleCredential.FromJson(json.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase CredentialJson không parse được (thiếu escape / JSON hỏng?). Thử nguồn khác.");
            }
        }

        var b64 = _configuration["Firebase:CredentialJsonBase64"];
        if (!string.IsNullOrWhiteSpace(b64))
        {
            try
            {
                var bytes = Convert.FromBase64String(b64.Trim());
                return GoogleCredential.FromJson(Encoding.UTF8.GetString(bytes));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase CredentialJsonBase64 không hợp lệ. Thử nguồn khác.");
            }
        }

        var credentialPath = _configuration["Firebase:CredentialPath"]?.Trim();
        if (!string.IsNullOrWhiteSpace(credentialPath))
        {
            foreach (var candidate in ResolveCredentialFileCandidates(credentialPath))
            {
                if (!File.Exists(candidate))
                    continue;
                try
                {
                    return GoogleCredential.FromFile(candidate);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Firebase CredentialPath đọc file thất bại: {Path}", candidate);
                }
            }

            _logger.LogWarning(
                "Firebase CredentialPath không tìm thấy file. Đã thử: {Candidates}. Trên Render Secret File, đường dẫn thường là /etc/secrets/&lt;tên file bạn đặt khi tạo secret&gt;.",
                string.Join(", ", ResolveCredentialFileCandidates(credentialPath)));
        }

        var gac = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (!string.IsNullOrWhiteSpace(gac))
        {
            if (File.Exists(gac))
            {
                try
                {
                    return GoogleCredential.FromFile(gac);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GOOGLE_APPLICATION_CREDENTIALS file lỗi: {Path}", gac);
                }
            }
            else
            {
                _logger.LogWarning("GOOGLE_APPLICATION_CREDENTIALS trỏ tới file không tồn tại: {Path}", gac);
            }
        }

        return null;
    }

    /// <summary>Render thường mount secret dưới <c>/etc/secrets/</c>; user có thể nhập full path hoặc chỉ tên file.</summary>
    private static IEnumerable<string> ResolveCredentialFileCandidates(string credentialPath)
    {
        var trimmed = credentialPath.Trim();
        yield return trimmed;

        var fileName = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(fileName))
            yield break;

        yield return Path.Combine("/etc/secrets", fileName);
        yield return Path.Combine("/etc/secrets", trimmed.TrimStart('/'));
    }
}
