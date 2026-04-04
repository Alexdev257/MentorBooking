namespace Shared.Contracts.Events;

/// <summary>
/// Published by BookingService when Zoom sends <c>recording.completed</c>.
/// MeetingService consumes this and inserts <c>meeting_recordings</c> row(s): video/audio and optional Zoom cloud transcript (VTT).
/// </summary>
public record ZoomRecordingCompletedEvent(
    Guid BookingId,
    string? StorageUrl,
    string? ContentType = null,
    int? DurationSeconds = null,
    long? SizeBytes = null,
    string? TranscriptStorageUrl = null,
    string? TranscriptContentType = null,
    long? TranscriptSizeBytes = null) : IntegrationEvent;
