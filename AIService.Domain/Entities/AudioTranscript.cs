namespace AIService.Domain.Entities;

using AIService.Domain.Enum;

using Shared.Kernel.Domain;

public class AudioTranscript : AuditableEntity
{

    public string Title { get; set; } = default!;
    public MediaSourceType SourceType { get; set; }

    public string? OriginalFileName { get; set; }
    public string? OriginalFilePath { get; set; }
    public string? ExtractedAudioPath { get; set; }
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }

    public string? RawText { get; set; }
    public string? CleanText { get; set; }

    public TranscriptStatus? Status { get; set; } = null;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public virtual ICollection<AudioTranscriptSegment> Segments { get; set; } = new List<AudioTranscriptSegment>();
}
