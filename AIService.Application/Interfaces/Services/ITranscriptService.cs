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

    /// <summary>Download video/audio from a URL, save locally, and queue for Whisper transcription + Gemini summarization.</summary>
    Task<CommonResponse<TranscriptUploadResponseDto>> UploadFromUrlAsync(
        string url, string? title, int sourceType, string? contentType,
        Guid? userId, CancellationToken cancellationToken = default);

    /// <summary>Persist transcript text/segments already produced by Zoom audio transcript.</summary>
    Task<CommonResponse<ZoomAudioTranscriptIngestResponseDto>> IngestZoomAudioTranscriptAsync(
        ZoomAudioTranscriptIngestRequestDto request,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>Process a queued transcript: FFmpeg extract → Whisper transcribe → Gemini summarize.</summary>
    Task ProcessTranscriptAsync(Guid transcriptId, CancellationToken cancellationToken = default);

    /// <summary>Get IDs of transcripts waiting to be processed.</summary>
    Task<List<Guid>> GetQueuedTranscriptIdsAsync(CancellationToken cancellationToken = default);
}
