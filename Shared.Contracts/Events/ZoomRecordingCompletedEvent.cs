namespace Shared.Contracts.Events;

/// <summary>
/// Published by BookingService when Zoom sends <c>recording.completed</c>.
/// MeetingService consumes this and inserts a <c>meeting_recordings</c> row.
/// </summary>
public record ZoomRecordingCompletedEvent(
    Guid BookingId,
    string StorageUrl,
    string? ContentType = null,
    int? DurationSeconds = null,
    long? SizeBytes = null) : IntegrationEvent;
