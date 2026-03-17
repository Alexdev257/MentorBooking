namespace AIService.Application.Interfaces;

public interface IMediaProcessingService
{
    Task<string> ExtractAudioToWavAsync(string inputFilePath, CancellationToken cancellationToken = default);
}
