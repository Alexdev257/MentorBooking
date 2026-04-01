namespace AIService.Application.Interfaces;

public interface IMediaProcessingService
{
    Task<string> ExtractAudioToWavAsync(string inputFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract/re-encode to a small file for Groq Whisper (MP3 speech preset) to avoid HTTP 413.
    /// </summary>
    Task<string> ExtractAudioForTranscriptionAsync(string inputFilePath, CancellationToken cancellationToken = default);
}
