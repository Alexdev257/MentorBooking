using Shared.Kernel.Domain;

namespace AIService.Domain.Entities;

public class AudioTranscriptSegment : AuditableEntity
{
    public Guid AudioTranscriptId { get; set; }
    public virtual AudioTranscript AudioTranscript { get; set; } = default!;

    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string Text { get; set; } = default!;
}
