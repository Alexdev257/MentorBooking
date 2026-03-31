using System.Text.Json.Serialization;

namespace BookingService.Application.DTOs.Request;

public class ZoomWebhookRequest
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("event_ts")]
    public long EventTs { get; set; }

    [JsonPropertyName("payload")]
    public ZoomWebhookPayload Payload { get; set; } = new();
}

public class ZoomWebhookPayload
{
    [JsonPropertyName("plainToken")]
    public string? PlainToken { get; set; }

    [JsonPropertyName("object")]
    public ZoomWebhookObject? Object { get; set; }
}

public class ZoomWebhookObject
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(ZoomJsonStringOrNumberConverter))]
    public string? Id { get; set; }

    /// <summary>Instance UUID for this meeting occurrence; required for <c>GET /past_meetings/{{uuid}}/participants</c>.</summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("start_time")]
    public DateTime? StartTime { get; set; }

    [JsonPropertyName("host_id")]
    [JsonConverter(typeof(ZoomJsonStringOrNumberConverter))]
    public string? HostId { get; set; }

    [JsonPropertyName("recording_files")]
    public List<ZoomRecordingFile>? RecordingFiles { get; set; }
}

public class ZoomRecordingFile
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(ZoomJsonStringOrNumberConverter))]
    public string? Id { get; set; }

    [JsonPropertyName("meeting_id")]
    [JsonConverter(typeof(ZoomJsonStringOrNumberConverter))]
    public string? MeetingId { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    [JsonPropertyName("play_url")]
    public string? PlayUrl { get; set; }

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }

    [JsonPropertyName("recording_start")]
    public DateTime? RecordingStart { get; set; }

    [JsonPropertyName("recording_end")]
    public DateTime? RecordingEnd { get; set; }
}

public class ZoomCrcResponse
{
    [JsonPropertyName("plainToken")]
    public string PlainToken { get; set; } = string.Empty;

    [JsonPropertyName("encryptedToken")]
    public string EncryptedToken { get; set; } = string.Empty;
}
