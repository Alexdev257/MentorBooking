using AIService.Domain.Entities;
using Shared.Kernel.Interfaces;

namespace AIService.Application.Interfaces.Repositories;

public interface IAIUnitOfWork : IUnitOfWork
{
    IGenericRepository<ActionItem> ActionItems { get; }
    IGenericRepository<MeetingSummary> MeetingSummaries { get; }
    IGenericRepository<AudioTranscript> Transcripts { get; }
    IGenericRepository<AudioTranscriptSegment> TranscriptSegments { get; }
}
