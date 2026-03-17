namespace AIService.Application.DTOs.Transcripts;

public class TranscriptListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
