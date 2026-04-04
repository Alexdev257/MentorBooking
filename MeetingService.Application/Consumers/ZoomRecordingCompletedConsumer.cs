using MassTransit;
using MeetingService.Application.Interfaces.Repositories;
using MeetingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace MeetingService.Application.Consumers;

public class ZoomRecordingCompletedConsumer : IConsumer<ZoomRecordingCompletedEvent>
{
    private readonly IMeetingUnitOfWork _unitOfWork;
    private readonly ILogger<ZoomRecordingCompletedConsumer> _logger;

    public ZoomRecordingCompletedConsumer(IMeetingUnitOfWork unitOfWork, ILogger<ZoomRecordingCompletedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ZoomRecordingCompletedEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "ZoomRecordingCompleted: BookingId={BookingId}, RecordingUrlLen={Len}, TranscriptUrlLen={Tlen}",
            message.BookingId,
            message.StorageUrl?.Length ?? 0,
            message.TranscriptStorageUrl?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(message.StorageUrl) && string.IsNullOrWhiteSpace(message.TranscriptStorageUrl))
        {
            _logger.LogWarning("ZoomRecordingCompleted: no recording or transcript URL for BookingId {BookingId}", message.BookingId);
            return;
        }

        var meeting = await _unitOfWork.Meetings
            .FindAsync(m => m.BookingId == message.BookingId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (meeting == null)
        {
            _logger.LogWarning(
                "ZoomRecordingCompleted: no Meeting for BookingId {BookingId}; nothing stored.",
                message.BookingId);
            return;
        }

        var anyAdded = false;

        if (!string.IsNullOrWhiteSpace(message.StorageUrl))
            anyAdded |= await TryAddRecordingAsync(
                meeting.Id,
                message.StorageUrl.Trim(),
                message.ContentType,
                message.DurationSeconds,
                message.SizeBytes,
                ct);

        if (!string.IsNullOrWhiteSpace(message.TranscriptStorageUrl))
            anyAdded |= await TryAddRecordingAsync(
                meeting.Id,
                message.TranscriptStorageUrl.Trim(),
                message.TranscriptContentType,
                null,
                message.TranscriptSizeBytes,
                ct);

        if (anyAdded)
            await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<bool> TryAddRecordingAsync(
        Guid meetingId,
        string storageUrl,
        string? contentType,
        int? durationSeconds,
        long? sizeBytes,
        CancellationToken ct)
    {
        var exists = await _unitOfWork.MeetingRecordings
            .FindAsync(r => r.MeetingId == meetingId && r.StorageUrl == storageUrl)
            .AnyAsync(ct);

        if (exists)
        {
            _logger.LogInformation(
                "ZoomRecordingCompleted: duplicate URL skipped for MeetingId {MeetingId}",
                meetingId);
            return false;
        }

        var recording = new MeetingRecording
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            Status = 1,
            StorageUrl = storageUrl,
            ContentType = contentType,
            DurationSeconds = durationSeconds,
            SizeBytes = sizeBytes,
        };

        await _unitOfWork.MeetingRecordings.AddAsync(recording);
        _logger.LogInformation(
            "ZoomRecordingCompleted: saved MeetingRecording {RecordingId} for MeetingId {MeetingId} ContentType={Ct}",
            recording.Id,
            meetingId,
            contentType);
        return true;
    }
}
