namespace AIService.Application.DTOs.Transcripts;

public class TranscriptSegmentDto
{
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string Text { get; set; } = string.Empty;
}
