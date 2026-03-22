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
    private readonly IMediaProcessingService _mediaProcessing;
    private readonly ITranscriptionService _transcription;
    private readonly ITranscriptSummarizationService _summarization;
    private readonly ILogger<TranscriptService> _logger;

    private static readonly HashSet<string> VideoMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo"
    };

    public TranscriptService(
        IAIUnitOfWork unitOfWork,
        IMapper mapper,
        IFileStorageService fileStorage,
        IMediaProcessingService mediaProcessing,
        ITranscriptionService transcription,
        ITranscriptSummarizationService summarization,
        ILogger<TranscriptService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _mediaProcessing = mediaProcessing;
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
            Status = (TranscriptStatus)1,
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

        TranscriptSummaryDto? uploadSummary = null;
        try
        {
            entity.Status = (TranscriptStatus)2;
            _unitOfWork.Transcripts.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var inputPath = entity.OriginalFilePath!;
            if (IsVideo(contentType))
            {
                try
                {
                    entity.ExtractedAudioPath = await _mediaProcessing.ExtractAudioToWavAsync(inputPath, cancellationToken);
                    inputPath = entity.ExtractedAudioPath;
                }
                catch (Exception ex)
                {
                    entity.Status = (TranscriptStatus)4;
                    entity.ErrorMessage = "Tách audio thất bại: " + ex.Message;
                    _unitOfWork.Transcripts.UpdateAsync(entity);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    response.IsSuccess = false;
                    response.Data = new TranscriptUploadResponseDto { Id = entity.Id, Status = (int)entity.Status, Message = entity.ErrorMessage ?? "Lỗi" };
                    response.Message = "Upload thành công nhưng tách audio thất bại.";
                    return response;
                }
            }

            var result = await _transcription.TranscribeAsync(inputPath, cancellationToken);
            entity.RawText = result.FullText;
            entity.CleanText = result.FullText;
            entity.Status = (TranscriptStatus)3;
            entity.ProcessedAtUtc = DateTime.UtcNow;
            entity.ErrorMessage = null;

            foreach (var seg in result.Segments)
            {
                var segment = new AudioTranscriptSegment
                {
                    Id = Guid.NewGuid(),
                    AudioTranscriptId = entity.Id,
                    StartSeconds = seg.StartSeconds,
                    EndSeconds = seg.EndSeconds,
                    Text = seg.Text,
                    CreatedBy = userId
                };
                await _unitOfWork.TranscriptSegments.AddAsync(segment);
            }
            _unitOfWork.Transcripts.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (_summarization.IsEnabled && !string.IsNullOrWhiteSpace(entity.CleanText))
            {
                try
                {
                    var sum = await _summarization.SummarizeAsync(entity.CleanText, entity.Title, cancellationToken);
                    if (sum != null)
                    {
                        await UpsertMeetingSummaryAsync(entity.Id, sum, userId, cancellationToken);
                        uploadSummary = sum;
                    }
                }
                catch (Exception sumEx)
                {
                    _logger.LogWarning(sumEx, "Summarization failed for transcript {TranscriptId}", entity.Id);
                }
            }
        }
        catch (Exception ex)
        {
            entity.Status = (TranscriptStatus)4;
            entity.ErrorMessage = ex.Message;
            _unitOfWork.Transcripts.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var isCompleted = entity.Status == (TranscriptStatus)3;
        response.IsSuccess = isCompleted;
        response.Data = new TranscriptUploadResponseDto
        {
            Id = entity.Id,
            Status = (int)entity.Status,
            Message = isCompleted
                ? "Upload và transcribe thành công."
                : entity.Status == (TranscriptStatus)4
                    ? (entity.ErrorMessage ?? "Lỗi")
                    : "Đang xử lý.",

            // Trả kết quả transcript ngay trong response upload
            FullText = isCompleted ? entity.CleanText : null,
            Segments = isCompleted
                ? (await _unitOfWork.TranscriptSegments
                    .FindAsync(s => s.AudioTranscriptId == entity.Id)
                    .OrderBy(s => s.StartSeconds)
                    .ToListAsync(cancellationToken))
                    .Select(s => new TranscriptSegmentDto
                    {
                        StartSeconds = s.StartSeconds,
                        EndSeconds   = s.EndSeconds,
                        Text         = s.Text
                    }).ToList()
                : new List<TranscriptSegmentDto>(),
            Summary = isCompleted ? uploadSummary : null
        };
        response.Message = response.IsSuccess
            ? "Upload thành công."
            : entity.Status == (TranscriptStatus)4
                ? "Upload xong nhưng transcribe lỗi."
                : "Đang xử lý.";
        return response;
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

        var sum = await _summarization.SummarizeAsync(text, entity.Title, cancellationToken);
        if (sum == null)
        {
            response.IsSuccess = false;
            response.Message = "Tóm tắt thất bại (Gemini không trả kết quả hợp lệ).";
            return response;
        }

        await UpsertMeetingSummaryAsync(transcriptId, sum, userId, cancellationToken);
        response.IsSuccess = true;
        response.Data = sum;
        response.Message = "Thành công.";
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

    private static bool IsVideo(string? contentType) => contentType != null && VideoMimeTypes.Contains(contentType);
}
