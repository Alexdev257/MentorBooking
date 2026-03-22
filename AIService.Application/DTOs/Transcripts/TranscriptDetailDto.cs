namespace AIService.Application.DTOs.Transcripts;

public class TranscriptDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? RawText { get; set; }
    public string? CleanText { get; set; }
    public List<TranscriptSegmentDto> Segments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Gemini-generated summary when available.</summary>
    public TranscriptSummaryDto? Summary { get; set; }
}
