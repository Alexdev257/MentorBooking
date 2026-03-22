using AIService.Application.DTOs.Transcripts;
using Shared.Contracts.Common.Wrappers;

namespace AIService.Application.Interfaces.Services;

public interface ITranscriptService
{
    Task<CommonResponse<TranscriptUploadResponseDto>> UploadAsync(string title, int sourceType, Stream fileStream, string fileName, string contentType, long fileSizeBytes, Guid? userId, CancellationToken cancellationToken = default);
    Task<CommonResponse<TranscriptDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CommonResponse<List<TranscriptListItemDto>>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Runs Gemini summarization for an existing transcript and persists a MeetingSummary row (MeetingId = transcript id).</summary>
    Task<CommonResponse<TranscriptSummaryDto>> SummarizeAsync(Guid transcriptId, Guid? userId, CancellationToken cancellationToken = default);
}
