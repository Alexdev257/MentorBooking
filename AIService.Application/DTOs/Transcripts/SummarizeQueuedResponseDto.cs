namespace AIService.Application.DTOs.Transcripts;

/// <summary>Returned when POST /summarize accepts work for background processing (HTTP 202).</summary>
public class SummarizeQueuedResponseDto
{
    public Guid TranscriptId { get; set; }
    /// <summary>Matches <see cref="AIService.Domain.Enum.SummaryQueueStatus"/>.</summary>
    public int SummaryQueueStatus { get; set; }
    public string Message { get; set; } = string.Empty;
}
