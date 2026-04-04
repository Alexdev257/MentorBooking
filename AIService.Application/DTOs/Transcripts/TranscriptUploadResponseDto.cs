namespace AIService.Application.DTOs.Transcripts;

public class TranscriptUploadResponseDto
{
    public Guid Id { get; set; }
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;

    // Kết quả transcript từ Whisper (null nếu chưa xử lý xong hoặc lỗi)
    public string? FullText { get; set; }
    public List<TranscriptSegmentDto> Segments { get; set; } = new();

    /// <summary>Populated when Gemini summarization succeeds after transcribe.</summary>
    public TranscriptSummaryDto? Summary { get; set; }
}
