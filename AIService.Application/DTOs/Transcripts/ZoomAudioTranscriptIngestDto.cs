namespace AIService.Application.DTOs.Transcripts;

public class ZoomAudioTranscriptIngestRequestDto
{
    public Guid BookingId { get; set; }
    public string MeetingId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SourceFileName { get; set; }
    public string? SourceUrl { get; set; }
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string CleanText { get; set; } = string.Empty;
    public List<ZoomAudioTranscriptSegmentDto> Segments { get; set; } = new();
}

public class ZoomAudioTranscriptSegmentDto
{
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class ZoomAudioTranscriptIngestResponseDto
{
    public Guid TranscriptId { get; set; }
}
