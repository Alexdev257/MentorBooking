using BookingService.Application.Interfaces.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Linq;

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
            LogCredentialDiagnostics();
            _logger.LogWarning(
                "MeetingRecordingCloudMirror: không load được Google credential cho Storage (JSON Base64, file /etc/secrets, hay GOOGLE_APPLICATION_CREDENTIALS). BucketName empty={BucketEmpty}. Chỉ lưu URL Zoom. Nếu log vẫn giống bản cũ 'chưa cấu hình', hãy deploy lại Docker booking-service.",
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
        string? zoomDownloadToken,
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
            var oauthToken = await _zoomService.GetAccessToken(cancellationToken);
            var accessToken = FirstNonEmpty(zoomDownloadToken, oauthToken);
            var authorizedDownloadUrl = BuildZoomAuthorizedDownloadUrl(zoomDownloadUrl.Trim(), accessToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, authorizedDownloadUrl);
            // For Zoom webhook_download URLs, query access_token (download_token) is often required.
            // Keep Bearer only when we don't have a dedicated download token.
            if (string.IsNullOrWhiteSpace(zoomDownloadToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);
            }

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

            // Some Zoom links can return HTML/login pages (status 200) if auth is missing after redirects.
            // Reject obvious non-media responses to avoid uploading invalid files that appear as 0:00 videos.
            var actualContentType = response.Content.Headers.ContentType?.MediaType;
            if (IsLikelyNonMediaResponse(actualContentType, contentType))
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Zoom download returned non-media content for booking {BookingId}. Expected={ExpectedType}, Actual={ActualType}, Url={Url}, BodyStart={BodyStart}",
                    bookingId,
                    contentType,
                    actualContentType ?? "(null)",
                    authorizedDownloadUrl,
                    body.Length > 300 ? body[..300] : body);
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

    private static string BuildZoomAuthorizedDownloadUrl(string baseUrl, string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return baseUrl;
        if (string.IsNullOrWhiteSpace(accessToken))
            return baseUrl;
        if (baseUrl.Contains("access_token=", StringComparison.OrdinalIgnoreCase))
            return baseUrl;

        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}access_token={Uri.EscapeDataString(accessToken)}";
    }

    private static bool IsLikelyNonMediaResponse(string? actualContentType, string? expectedContentType)
    {
        if (string.IsNullOrWhiteSpace(actualContentType))
            return false;

        var actual = actualContentType.Trim().ToLowerInvariant();
        if (actual.StartsWith("video/") || actual.StartsWith("audio/") || actual.StartsWith("text/vtt"))
            return false;

        var expected = (expectedContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (expected.StartsWith("video/") || expected.StartsWith("audio/"))
            return actual.StartsWith("text/") || actual.Contains("json") || actual.Contains("html");

        return false;
    }

    /// <summary>JSON → Base64 → đường file (nhiều biến env) → GOOGLE_APPLICATION_CREDENTIALS → quét /etc/secrets/*.json.</summary>
    private GoogleCredential? TryLoadGoogleCredential()
    {
        var json = FirstNonEmpty(
            _configuration["Firebase:CredentialJson"],
            Environment.GetEnvironmentVariable("Firebase__CredentialJson"));

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                return GoogleCredential.FromJson(json.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase CredentialJson không parse được (JSON hỏng hoặc env bị cắt). Thử nguồn khác hoặc dùng CredentialJsonBase64 / Secret File.");
            }
        }

        var b64 = FirstNonEmpty(
            _configuration["Firebase:CredentialJsonBase64"],
            Environment.GetEnvironmentVariable("Firebase__CredentialJsonBase64"));

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

        var credentialPath = FirstNonEmpty(
            _configuration["Firebase:CredentialPath"],
            Environment.GetEnvironmentVariable("Firebase__CredentialPath"));

        if (!string.IsNullOrWhiteSpace(credentialPath))
        {
            var candidates = ResolveCredentialFileCandidates(credentialPath).Distinct().ToList();
            foreach (var candidate in candidates)
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
                "Firebase CredentialPath không tìm thấy file hợp lệ. Đã thử: {Candidates}. Trên Render, mount Secret File thường là /etc/secrets/tên-file-bạn-chọn.",
                string.Join(", ", candidates));
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

        return TryLoadFromJsonFilesInEtcSecrets();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }

        return null;
    }

    /// <summary>Cuối cùng: thử mọi file .json trong /etc/secrets (Render Secret Files).</summary>
    private GoogleCredential? TryLoadFromJsonFilesInEtcSecrets()
    {
        const string secretsDir = "/etc/secrets";
        if (!Directory.Exists(secretsDir))
            return null;

        var files = Directory.GetFiles(secretsDir, "*.json", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
            return null;

        var ordered = files
            .OrderByDescending(f =>
            {
                var n = Path.GetFileName(f).ToLowerInvariant();
                if (n.Contains("firebase", StringComparison.Ordinal)) return 3;
                if (n.Contains("adminsdk", StringComparison.Ordinal)) return 2;
                if (n.Contains("service", StringComparison.Ordinal)) return 1;
                return 0;
            })
            .ThenBy(f => Path.GetFileName(f), StringComparer.Ordinal);

        foreach (var path in ordered)
        {
            try
            {
                return GoogleCredential.FromFile(path);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Bỏ qua file JSON trong /etc/secrets (không phải service account?): {Path}", path);
            }
        }

        return null;
    }

    private void LogCredentialDiagnostics()
    {
        var jsonCfg = _configuration["Firebase:CredentialJson"];
        var jsonEnv = Environment.GetEnvironmentVariable("Firebase__CredentialJson");
        var b64Cfg = _configuration["Firebase:CredentialJsonBase64"];
        var b64Env = Environment.GetEnvironmentVariable("Firebase__CredentialJsonBase64");
        var pathCfg = _configuration["Firebase:CredentialPath"];
        var pathEnv = Environment.GetEnvironmentVariable("Firebase__CredentialPath");
        var gac = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        string secretsList = "(không đọc được /etc/secrets)";
        try
        {
            if (Directory.Exists("/etc/secrets"))
            {
                var names = Directory.GetFiles("/etc/secrets", "*", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .OrderBy(n => n, StringComparer.Ordinal);
                secretsList = names.Any() ? string.Join(", ", names) : "(thư mục trống)";
            }
        }
        catch (Exception ex)
        {
            secretsList = ex.Message;
        }

        _logger.LogWarning(
            "Firebase credential diagnostics: BucketLen={BucketLen}, JsonCfgLen={JsonCfgLen}, JsonEnvLen={JsonEnvLen}, B64CfgLen={B64CfgLen}, B64EnvLen={B64EnvLen}, PathCfg={PathCfg}, PathEnv={PathEnv}, GAC={Gac}, FilesInEtcSecrets=[{Secrets}]",
            _bucketName.Length,
            jsonCfg?.Length ?? 0,
            jsonEnv?.Length ?? 0,
            b64Cfg?.Length ?? 0,
            b64Env?.Length ?? 0,
            pathCfg ?? "(null)",
            pathEnv ?? "(null)",
            gac ?? "(null)",
            secretsList);
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
