using AIService.Domain.Entities;
using Shared.Kernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIService.Application.Interfaces.Repositories
{
    public interface IAIUnitOfWork : IUnitOfWork
    {
        IGenericRepository<ActionItem> ActionItems { get; }
        IGenericRepository<MeetingSummary> MeetingSummaries { get; }
        IGenericRepository<Transcript> Transcripts { get; }
        IGenericRepository<TranscriptSegment> TranscriptSegments { get; }
    }
}
