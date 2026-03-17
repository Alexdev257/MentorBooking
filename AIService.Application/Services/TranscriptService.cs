using AIService.Application.DTOs.Summary;
using AIService.Application.DTOs.Transcripts;
using AIService.Application.Interfaces;
using AIService.Application.Interfaces.Repositories;
using AIService.Application.Interfaces.Services;
using AIService.Domain.Entities;
using AIService.Domain.Enum;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Common.Wrappers;
using System.Text.Json;

namespace AIService.Application.Services;

public class TranscriptService : ITranscriptService
{
    private readonly IAIUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorage;
    private readonly IMediaProcessingService _mediaProcessing;
    private readonly ITranscriptionService _transcription;
    private readonly ISummaryService _summaryService;

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
        ISummaryService summaryService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _mediaProcessing = mediaProcessing;
        _transcription = transcription;
        _summaryService = summaryService;
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
                : new List<TranscriptSegmentDto>()
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
        return new CommonResponse<TranscriptDetailDto> { IsSuccess = true, Data = dto, Message = "Thành công." };
    }

    public async Task<CommonResponse<List<TranscriptListItemDto>>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Transcripts.FindAsync(t => !t.IsDeleted).OrderByDescending(t => t.CreatedAt);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<TranscriptListItemDto>>(items);
        return new CommonResponse<List<TranscriptListItemDto>> { IsSuccess = true, Data = dtos, Message = "Thành công." };
    }

    public async Task<CommonResponse<SummaryResponseDto>> SummarizeAsync(Guid transcriptId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Transcripts.GetByIdAsync(transcriptId);
        if (entity == null || entity.IsDeleted)
            return new CommonResponse<SummaryResponseDto> { IsSuccess = false, Message = "Không tìm thấy transcript." };

        if (entity.Status != (TranscriptStatus)3)
            return new CommonResponse<SummaryResponseDto> { IsSuccess = false, Message = "Transcript chưa được xử lý xong." };

        if (string.IsNullOrWhiteSpace(entity.CleanText))
            return new CommonResponse<SummaryResponseDto> { IsSuccess = false, Message = "Transcript không có nội dung." };

        // Check if summary already exists
        var existing = await _unitOfWork.MeetingSummaries
            .FindAsync(s => s.MeetingId == transcriptId && !s.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            return new CommonResponse<SummaryResponseDto>
            {
                IsSuccess = true,
                Data = new SummaryResponseDto
                {
                    TranscriptId = transcriptId,
                    Summary = existing.Summary,
                    KeyPoints = JsonSerializer.Deserialize<List<string>>(existing.KeyPoints) ?? new(),
                    Topics = JsonSerializer.Deserialize<List<string>>(existing.Topics) ?? new(),
                    Sentiment = existing.Sentiment,
                    Model = existing.Model ?? string.Empty
                },
                Message = "Thành công."
            };
        }

        var result = await _summaryService.SummarizeAsync(entity.CleanText, cancellationToken);
        if (result == null)
            return new CommonResponse<SummaryResponseDto> { IsSuccess = false, Message = "Tóm tắt thất bại." };

        var summary = new MeetingSummary
        {
            Id = Guid.NewGuid(),
            MeetingId = transcriptId,
            Summary = result.Summary,
            KeyPoints = JsonSerializer.Serialize(result.KeyPoints),
            Topics = JsonSerializer.Serialize(result.Topics),
            Sentiment = result.Sentiment,
            Model = result.Model
        };

        await _unitOfWork.MeetingSummaries.AddAsync(summary);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CommonResponse<SummaryResponseDto> { IsSuccess = true, Data = result, Message = "Tóm tắt thành công." };
    }

    private static bool IsVideo(string? contentType) => contentType != null && VideoMimeTypes.Contains(contentType);
}
