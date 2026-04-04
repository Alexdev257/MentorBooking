using System.Net.Http.Json;
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
    private readonly IHttpClientFactory _httpClientFactory;
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
        IHttpClientFactory httpClientFactory,
        ILogger<TranscriptService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _mediaProcessing = mediaProcessing;
        _transcription = transcription;
        _summarization = summarization;
        _httpClientFactory = httpClientFactory;
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

            // Download file nếu chưa có (upload-from-url chỉ lưu URL, background mới download)
            if (string.IsNullOrWhiteSpace(entity.OriginalFilePath) && !string.IsNullOrWhiteSpace(entity.SourceUrl))
            {
                _logger.LogInformation("Downloading file from SourceUrl for transcript {TranscriptId}", transcriptId);
                var httpClient = _httpClientFactory.CreateClient("url-downloader");
                using var downloadResponse = await httpClient.GetAsync(entity.SourceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!downloadResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Không thể download file từ SourceUrl: HTTP {(int)downloadResponse.StatusCode}");

                var resolvedContentType = entity.MimeType
                    ?? downloadResponse.Content.Headers.ContentType?.MediaType
                    ?? "video/mp4";

                var fileName = entity.OriginalFileName ?? ResolveFileNameFromContentType(resolvedContentType);
                await using var stream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken);
                var savedPath = await _fileStorage.SaveAsync(stream, fileName, cancellationToken);
                entity.OriginalFilePath = savedPath;
                entity.FileSizeBytes = new FileInfo(savedPath).Length;
                _unitOfWork.Transcripts.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Downloaded file to {Path} for transcript {TranscriptId}", savedPath, transcriptId);
            }

            var transcribePath = entity.OriginalFilePath!;
            if (NeedsCompressedAudioForGroq(entity, transcribePath))
            {
                _logger.LogInformation("Encoding audio for Groq (avoid 413) for transcript {TranscriptId}", transcriptId);
                entity.ExtractedAudioPath = await _mediaProcessing.ExtractAudioForTranscriptionAsync(transcribePath, cancellationToken);
                _unitOfWork.Transcripts.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                transcribePath = entity.ExtractedAudioPath;
            }

            var result = await _transcription.TranscribeAsync(transcribePath, cancellationToken);
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
                entity.SummaryQueueStatus = SummaryQueueStatus.Pending;
                entity.SummaryRequestedBy = entity.CreatedBy;
                entity.SummaryQueueError = null;
                _unitOfWork.Transcripts.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed for transcript {TranscriptId}", transcriptId);
            entity.Status = TranscriptStatus.Failed;
            entity.ErrorMessage = FormatProcessingError(ex);
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

    public async Task<List<Guid>> GetPendingSummaryTranscriptIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Transcripts
            .FindAsync(t =>
                !t.IsDeleted &&
                t.Status == TranscriptStatus.Completed &&
                t.SummaryQueueStatus == SummaryQueueStatus.Pending)
            .OrderBy(t => t.UpdatedAt ?? t.CreatedAt)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessPendingSummaryAsync(Guid transcriptId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Transcripts.GetByIdAsync(transcriptId);
        if (entity == null || entity.SummaryQueueStatus != SummaryQueueStatus.Pending)
            return;

        entity.SummaryQueueStatus = SummaryQueueStatus.Processing;
        entity.SummaryQueueError = null;
        _unitOfWork.Transcripts.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            if (!_summarization.IsEnabled)
            {
                entity.SummaryQueueStatus = SummaryQueueStatus.Failed;
                entity.SummaryQueueError = "Gemini chưa được cấu hình (thiếu Gemini:ApiKey).";
                _unitOfWork.Transcripts.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var text = entity.CleanText ?? entity.RawText;
            if (string.IsNullOrWhiteSpace(text))
            {
                entity.SummaryQueueStatus = SummaryQueueStatus.Failed;
                entity.SummaryQueueError = "Transcript không có nội dung.";
                _unitOfWork.Transcripts.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var sum = await _summarization.SummarizeAsync(text, entity.Title, cancellationToken);
            if (sum == null)
            {
                entity.SummaryQueueStatus = SummaryQueueStatus.Failed;
                entity.SummaryQueueError = "Gemini không trả kết quả.";
                _unitOfWork.Transcripts.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            await UpsertMeetingSummaryAsync(entity.Id, sum, entity.SummaryRequestedBy, cancellationToken);
            entity.SummaryQueueStatus = SummaryQueueStatus.None;
            entity.SummaryQueueError = null;
            _unitOfWork.Transcripts.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Summary job failed for transcript {TranscriptId}", transcriptId);
            entity.SummaryQueueStatus = SummaryQueueStatus.Failed;
            entity.SummaryQueueError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            _unitOfWork.Transcripts.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
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

    public async Task<CommonResponse<SummarizeQueuedResponseDto>> QueueSummarizeAsync(Guid transcriptId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<SummarizeQueuedResponseDto>();
        var entity = await _unitOfWork.Transcripts.GetByIdAsync(transcriptId);
        if (entity == null || entity.IsDeleted)
        {
            response.IsSuccess = false;
            response.Message = "Không tìm thấy transcript.";
            return response;
        }

        if (entity.Status != TranscriptStatus.Completed)
        {
            response.IsSuccess = false;
            response.Message = "Transcript chưa hoàn tất xử lý.";
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

        if (entity.SummaryQueueStatus == SummaryQueueStatus.Processing)
        {
            response.IsSuccess = true;
            response.Data = new SummarizeQueuedResponseDto
            {
                TranscriptId = transcriptId,
                SummaryQueueStatus = (int)SummaryQueueStatus.Processing,
                Message = "Đang tóm tắt trong background."
            };
            response.Message = "Đang xử lý.";
            return response;
        }

        entity.SummaryQueueStatus = SummaryQueueStatus.Pending;
        entity.SummaryRequestedBy = userId;
        entity.SummaryQueueError = null;
        _unitOfWork.Transcripts.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        response.IsSuccess = true;
        response.Data = new SummarizeQueuedResponseDto
        {
            TranscriptId = transcriptId,
            SummaryQueueStatus = (int)SummaryQueueStatus.Pending,
            Message = "Đã thêm vào hàng đợi. Gọi GET để theo dõi summary hoặc summaryQueueStatus."
        };
        response.Message = "Đã nhận yêu cầu tóm tắt (xử lý background).";
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

        if (_summarization.IsEnabled && !string.IsNullOrWhiteSpace(cleanText))
        {
            transcript.SummaryQueueStatus = SummaryQueueStatus.Pending;
            transcript.SummaryRequestedBy = userId;
            transcript.SummaryQueueError = null;
            _unitOfWork.Transcripts.UpdateAsync(transcript);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        response.IsSuccess = true;
        response.Message = "Ingest zoom audio transcript thành công.";
        response.Data = new ZoomAudioTranscriptIngestResponseDto { TranscriptId = transcript.Id };
        return response;
    }

    public async Task<CommonResponse<TranscriptUploadResponseDto>> UploadFromUrlAsync(
        string url, string? title, int sourceType, string? contentType,
        Guid? userId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<TranscriptUploadResponseDto>();
        if (string.IsNullOrWhiteSpace(url))
        {
            response.IsSuccess = false;
            response.Message = "URL is required.";
            return response;
        }

        var sourceTypeEnum = sourceType is >= 1 and <= 4 ? (MediaSourceType)sourceType : MediaSourceType.RecordVideo;

        try
        {
            var resolvedContentType = contentType ?? "video/mp4";
            var uriPath = new Uri(url).AbsolutePath;
            var fileName = Path.GetFileName(uriPath);
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('.'))
                fileName = ResolveFileNameFromContentType(resolvedContentType);

            var entity = new AudioTranscript
            {
                Id = Guid.NewGuid(),
                Title = title ?? Path.GetFileNameWithoutExtension(fileName),
                SourceType = sourceTypeEnum,
                OriginalFileName = fileName,
                MimeType = resolvedContentType,
                SourceUrl = url,
                Status = TranscriptStatus.Queued,
                CreatedBy = userId
            };

            await _unitOfWork.Transcripts.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            response.IsSuccess = true;
            response.Data = new TranscriptUploadResponseDto
            {
                Id = entity.Id,
                Status = (int)TranscriptStatus.Queued,
                Message = "URL đã được nhận. Background worker sẽ download và xử lý transcript."
            };
            response.Message = "Upload from URL thành công.";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadFromUrl failed for url {Url}", url);
            response.IsSuccess = false;
            response.Message = $"Lỗi khi lưu yêu cầu: {ex.Message}";
            return response;
        }
    }

    private static string ResolveFileNameFromContentType(string contentType) => contentType switch
    {
        "audio/mp4" or "audio/m4a" => "recording.m4a",
        "audio/mpeg" => "recording.mp3",
        _ => "recording.mp4"
    };

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
            existing.Report = dto.ReportJson;
            existing.Mindmap = dto.MindmapJson;
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
                Report = dto.ReportJson,
                Mindmap = dto.MindmapJson,
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
            ReportJson = entity.Report,
            MindmapJson = entity.Mindmap,
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

    private static bool NeedsCompressedAudioForGroq(AudioTranscript entity, string filePath)
    {
        if (IsVideo(entity.MimeType))
            return true;
        var ext = Path.GetExtension(entity.OriginalFileName ?? "").ToLowerInvariant();
        if (ext is ".mp4" or ".webm" or ".mov" or ".avi" or ".mkv")
            return true;
        try
        {
            var len = new FileInfo(filePath).Length;
            const long maxBytesBeforeReencode = 20L * 1024 * 1024;
            return len > maxBytesBeforeReencode;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatProcessingError(Exception ex)
    {
        var msg = ex.Message;
        if (ex.InnerException != null)
            msg += " → " + ex.InnerException.Message;
        return msg.Length > 2000 ? msg[..2000] : msg;
    }

}
