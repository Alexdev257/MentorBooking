namespace MeetingService.Application.DTOs;

public class MeetingListItemDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public int Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? JoinUrl { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RecordingsCount { get; set; }
}

public class MeetingDetailDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public int Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? JoinUrl { get; set; }
    public string? HostUrl { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MeetingRecordingDto> Recordings { get; set; } = new();
}

/// <summary>Chỉ link họp — tiện cho màn hình “Vào Zoom”.</summary>
public class MeetingJoinLinksDto
{
    public Guid MeetingId { get; set; }
    public Guid BookingId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? JoinUrl { get; set; }
    public string? HostUrl { get; set; }
}

public class MeetingRecordingDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public int Status { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public int? DurationSeconds { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
