namespace Shared.Contracts.Events;

public record ZoomRecordingProcessingRequestedEvent(
    Guid BookingId,
    string MeetingId,
    string RecordingDownloadUrl,
    string? ZoomDownloadToken,
    string ContentType,
    string Extension,
    int? DurationSeconds,
    long? SizeBytes) : IntegrationEvent;

