using System.Text.Json;
using AIService.Application.DTOs.Transcripts;
using AIService.Application.Interfaces;
using AIService.Application.Interfaces.Repositories;
using AIService.Application.Interfaces.Services;
using AIService.Domain.Entities;
using AIService.Domain.Enum;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Common.Wrappers;

namespace AIService.Application.Services;

public class TranscriptService : ITranscriptService
{
    private readonly IAIUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorage;
    private readonly ITranscriptionService _transcription;
    private readonly ITranscriptSummarizationService _summarization;
    private readonly ILogger<TranscriptService> _logger;

    public TranscriptService(
        IAIUnitOfWork unitOfWork,
        IMapper mapper,
        IFileStorageService fileStorage,
        ITranscriptionService transcription,
        ITranscriptSummarizationService summarization,
        ILogger<TranscriptService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _transcription = transcription;
        _summarization = summarization;
        _logger = logger;
    }

    public async Task<CommonResponse<TranscriptUploadResponseDto>> UploadAsync(string title, int sourceType, Stream fileStream, string fileName, string contentType, long fileSizeBytes, Guid? userId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<TranscriptUploadResponseDto>();
        var sourceTypeEnum = sourceType is >= 1 and <= 4 ? (MediaSourceType)sourceType : MediaSourceType.UploadVideo;

        var entity = new AudioTranscript
        {
            Id = Guid.NewGuid(),
            Title = title,
            SourceType = sourceTypeEnum,
            OriginalFileName = fileName,
            MimeType = contentType,
            FileSizeBytes = fileSizeBytes,
            Status = TranscriptStatus.Queued,
            CreatedBy = userId
        };

        try
        {
            var savedPath = await _fileStorage.SaveAsync(fileStream, fileName, cancellationToken);
            entity.OriginalFilePath = savedPath;
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.Message = "Lưu file thất bại.";
            response.ListErrors.Add(new Errors { Field = "File", Detail = ex.Message });
            return response;
        }

        await _unitOfWork.Transcripts.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        response.IsSuccess = true;
        response.Data = new TranscriptUploadResponseDto
        {
            Id = entity.Id,
            Status = (int)TranscriptStatus.Queued,
            Message = "File đã được upload. Đang chờ xử lý trong background."
        };
        response.Message = "Upload thành công. Transcript sẽ được xử lý trong background.";
        return response;
    }

    public async Task ProcessTranscriptAsync(Guid transcriptId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Transcripts.GetByIdAsync(transcriptId);
        if (entity == null || entity.Status != TranscriptStatus.Queued)
            return;

        try
        {
            entity.Status = TranscriptStatus.Processing;
            _unitOfWork.Transcripts.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var result = await _transcription.TranscribeAsync(entity.OriginalFilePath!, cancellationToken);
            entity.RawText = result.FullText;
            entity.CleanText = result.FullText;
            entity.Status = TranscriptStatus.Completed;
            entity.ProcessedAtUtc = DateTime.UtcNow;
            entity.ErrorMessage = null;

            foreach (var seg in result.Segments)
            {
                await _unitOfWork.TranscriptSegments.AddAsync(new AudioTranscriptSegment
                {
                    Id = Guid.NewGuid(),
                    AudioTranscriptId = entity.Id,
                    StartSeconds = seg.StartSeconds,
                    EndSeconds = seg.EndSeconds,
                    Text = seg.Text,
                    CreatedBy = entity.CreatedBy
                });
            }
            _unitOfWork.Transcripts.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (_summarization.IsEnabled && !string.IsNullOrWhiteSpace(entity.CleanText))
            {
                try
                {
                    var sum = await _summarization.SummarizeAsync(entity.CleanText, entity.Title, cancellationToken);
                    if (sum != null)
                        await UpsertMeetingSummaryAsync(entity.Id, sum, entity.CreatedBy, cancellationToken);
                }
                catch (Exception sumEx)
                {
                    _logger.LogWarning(sumEx, "Summarization failed for transcript {TranscriptId}", entity.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed for transcript {TranscriptId}", transcriptId);
            entity.Status = TranscriptStatus.Failed;
            entity.ErrorMessage = ex.Message;
            _unitOfWork.Transcripts.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<Guid>> GetQueuedTranscriptIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Transcripts
            .FindAsync(t => t.Status == TranscriptStatus.Queued && !t.IsDeleted)
            .OrderBy(t => t.CreatedAt)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CommonResponse<TranscriptDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Transcripts.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted)
        {
            return new CommonResponse<TranscriptDetailDto> { IsSuccess = false, Message = "Không tìm thấy transcript." };
        }
        var segments = await _unitOfWork.TranscriptSegments.FindAsync(s => s.AudioTranscriptId == id).OrderBy(s => s.StartSeconds).ToListAsync(cancellationToken);
        var dto = _mapper.Map<TranscriptDetailDto>(entity);
        dto.Segments = _mapper.Map<List<TranscriptSegmentDto>>(segments);
        var summaryEntity = await _unitOfWork.MeetingSummaries
            .FindAsync(m => m.MeetingId == id && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        dto.Summary = MapMeetingSummaryToDto(summaryEntity);
        return new CommonResponse<TranscriptDetailDto> { IsSuccess = true, Data = dto, Message = "Thành công." };
    }

    public async Task<CommonResponse<List<TranscriptListItemDto>>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Transcripts.FindAsync(t => !t.IsDeleted).OrderByDescending(t => t.CreatedAt);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<TranscriptListItemDto>>(items);
        return new CommonResponse<List<TranscriptListItemDto>> { IsSuccess = true, Data = dtos, Message = "Thành công." };
    }

    public async Task<CommonResponse<TranscriptSummaryDto>> SummarizeAsync(Guid transcriptId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<TranscriptSummaryDto>();
        var entity = await _unitOfWork.Transcripts.GetByIdAsync(transcriptId);
        if (entity == null || entity.IsDeleted)
        {
            response.IsSuccess = false;
            response.Message = "Không tìm thấy transcript.";
            return response;
        }

        var text = entity.CleanText ?? entity.RawText;
        if (string.IsNullOrWhiteSpace(text))
        {
            response.IsSuccess = false;
            response.Message = "Transcript chưa có nội dung để tóm tắt.";
            return response;
        }

        if (!_summarization.IsEnabled)
        {
            response.IsSuccess = false;
            response.Message = "Gemini chưa được cấu hình (thiếu Gemini:ApiKey).";
            return response;
        }

        TranscriptSummaryDto? sum;
        try
        {
            sum = await _summarization.SummarizeAsync(text, entity.Title, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Summarization failed for transcript {TranscriptId}", transcriptId);
            response.IsSuccess = false;
            response.Message = $"Tóm tắt thất bại: {ex.Message}";
            return response;
        }

        if (sum == null)
        {
            response.IsSuccess = false;
            response.Message = "Tóm tắt thất bại (Gemini không trả kết quả).";
            return response;
        }

        await UpsertMeetingSummaryAsync(transcriptId, sum, userId, cancellationToken);
        response.IsSuccess = true;
        response.Data = sum;
        response.Message = "Thành công.";
        return response;
    }

    public async Task<CommonResponse<ZoomAudioTranscriptIngestResponseDto>> IngestZoomAudioTranscriptAsync(
        ZoomAudioTranscriptIngestRequestDto request,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<ZoomAudioTranscriptIngestResponseDto>();

        if (string.IsNullOrWhiteSpace(request.RawText) && string.IsNullOrWhiteSpace(request.CleanText))
        {
            response.IsSuccess = false;
            response.Message = "Transcript text is required.";
            return response;
        }

        var cleanText = string.IsNullOrWhiteSpace(request.CleanText) ? request.RawText.Trim() : request.CleanText.Trim();
        var rawText = string.IsNullOrWhiteSpace(request.RawText) ? cleanText : request.RawText.Trim();
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"Zoom audio transcript {request.MeetingId}".Trim()
            : request.Title.Trim();

        var transcript = new AudioTranscript
        {
            Id = Guid.NewGuid(),
            Title = title,
            SourceType = MediaSourceType.RecordAudio,
            OriginalFileName = request.SourceFileName,
            OriginalFilePath = request.SourceUrl,
            MimeType = request.MimeType,
            FileSizeBytes = request.FileSizeBytes,
            RawText = rawText,
            CleanText = cleanText,
            Status = TranscriptStatus.Completed,
            ProcessedAtUtc = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _unitOfWork.Transcripts.AddAsync(transcript);

        foreach (var seg in request.Segments.OrderBy(s => s.StartSeconds))
        {
            if (string.IsNullOrWhiteSpace(seg.Text))
                continue;

            var start = seg.StartSeconds < 0 ? 0 : seg.StartSeconds;
            var end = seg.EndSeconds < start ? start : seg.EndSeconds;
            await _unitOfWork.TranscriptSegments.AddAsync(new AudioTranscriptSegment
            {
                Id = Guid.NewGuid(),
                AudioTranscriptId = transcript.Id,
                StartSeconds = start,
                EndSeconds = end,
                Text = seg.Text.Trim(),
                CreatedBy = userId
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Ingest zoom audio transcript thành công.";
        response.Data = new ZoomAudioTranscriptIngestResponseDto { TranscriptId = transcript.Id };
        return response;
    }

    private async Task UpsertMeetingSummaryAsync(Guid transcriptId, TranscriptSummaryDto dto, Guid? userId, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.MeetingSummaries
            .FindAsync(m => m.MeetingId == transcriptId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var keyPointsJson = JsonSerializer.Serialize(dto.KeyPoints);
        var topicsJson = JsonSerializer.Serialize(dto.Topics);
        var sentiment = string.IsNullOrWhiteSpace(dto.SentimentJson) ? "{}" : dto.SentimentJson;

        if (existing != null)
        {
            existing.Summary = dto.Summary;
            existing.KeyPoints = keyPointsJson;
            existing.Topics = topicsJson;
            existing.Sentiment = sentiment;
            existing.Model = dto.Model;
            existing.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.MeetingSummaries.UpdateAsync(existing);
        }
        else
        {
            var row = new MeetingSummary
            {
                Id = Guid.NewGuid(),
                MeetingId = transcriptId,
                Summary = dto.Summary,
                KeyPoints = keyPointsJson,
                Topics = topicsJson,
                Sentiment = sentiment,
                Model = dto.Model,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            await _unitOfWork.MeetingSummaries.AddAsync(row);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static TranscriptSummaryDto? MapMeetingSummaryToDto(MeetingSummary? entity)
    {
        if (entity == null)
            return null;
        return new TranscriptSummaryDto
        {
            Summary = entity.Summary ?? string.Empty,
            KeyPoints = DeserializeStringList(entity.KeyPoints),
            Topics = DeserializeStringList(entity.Topics),
            SentimentJson = string.IsNullOrWhiteSpace(entity.Sentiment) ? "{}" : entity.Sentiment,
            Model = entity.Model,
            GeneratedAtUtc = entity.UpdatedAt ?? entity.CreatedAt
        };
    }

    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

}
