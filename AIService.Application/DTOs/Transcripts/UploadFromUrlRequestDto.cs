namespace AIService.Application.DTOs.Transcripts;

public class UploadFromUrlRequestDto
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int SourceType { get; set; } = 3; // RecordVideo
    public string? ContentType { get; set; }
}
