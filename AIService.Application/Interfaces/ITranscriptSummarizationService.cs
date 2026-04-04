using AIService.Application.DTOs.Transcripts;

namespace AIService.Application.Interfaces;

/// <summary>
/// Optional LLM summarization for transcript text (e.g. Gemini). Disabled when API key is not configured.
/// </summary>
public interface ITranscriptSummarizationService
{
    bool IsEnabled { get; }

    Task<TranscriptSummaryDto?> SummarizeAsync(string transcriptText, string? title, CancellationToken cancellationToken = default);
}
