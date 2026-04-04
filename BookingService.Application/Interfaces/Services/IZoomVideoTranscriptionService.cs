namespace BookingService.Application.Interfaces.Services;

public interface IZoomVideoTranscriptionService
{
    /// <summary>
    /// Triggers AIService to download the video from <paramref name="videoUrl"/>,
    /// transcribe it with Whisper, and summarize with Gemini.
    /// Returns the AIService transcript ID, or null if the call failed.
    /// </summary>
    Task<Guid?> TriggerTranscriptionAsync(
        Guid bookingId,
        string videoUrl,
        string title,
        string? contentType,
        CancellationToken cancellationToken = default);
}
