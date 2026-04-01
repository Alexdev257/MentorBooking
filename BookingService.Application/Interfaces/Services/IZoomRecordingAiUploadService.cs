namespace BookingService.Application.Interfaces.Services;

public interface IZoomRecordingAiUploadService
{
    Task<bool> UploadRecordingToAiAsync(
        Guid bookingId,
        string meetingId,
        string recordingDownloadUrl,
        string? zoomDownloadToken,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
