using AIService.Application.DTOs.Transcripts;

namespace AIService.Application.Interfaces;

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default);
}
