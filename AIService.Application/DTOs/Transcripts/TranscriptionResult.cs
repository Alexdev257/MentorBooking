namespace AIService.Application.DTOs.Transcripts;

public class TranscriptionResult
{
    public string FullText { get; set; } = string.Empty;
    public List<TranscriptSegmentDto> Segments { get; set; } = new();
}
