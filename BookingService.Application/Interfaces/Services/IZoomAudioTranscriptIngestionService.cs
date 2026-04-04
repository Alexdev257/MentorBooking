namespace BookingService.Application.Interfaces.Services;

public interface IZoomAudioTranscriptIngestionService
{
    Task<Guid?> IngestZoomAudioTranscriptAsync(
        Guid bookingId,
        string meetingId,
        string transcriptDownloadUrl,
        string? zoomDownloadToken,
        string? sourceFileName,
        string? mimeType,
        long? fileSizeBytes,
        CancellationToken cancellationToken = default);
}
