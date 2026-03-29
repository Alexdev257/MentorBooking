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
            "ZoomRecordingCompleted: BookingId={BookingId}, UrlLength={Len}",
            message.BookingId,
            message.StorageUrl?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(message.StorageUrl))
        {
            _logger.LogWarning("ZoomRecordingCompleted: empty StorageUrl for BookingId {BookingId}", message.BookingId);
            return;
        }

        var meeting = await _unitOfWork.Meetings
            .FindAsync(m => m.BookingId == message.BookingId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (meeting == null)
        {
            _logger.LogWarning(
                "ZoomRecordingCompleted: no Meeting for BookingId {BookingId}; recording not stored.",
                message.BookingId);
            return;
        }

        var already = await _unitOfWork.MeetingRecordings
            .FindAsync(r => r.MeetingId == meeting.Id && r.StorageUrl == message.StorageUrl)
            .AnyAsync(ct);

        if (already)
        {
            _logger.LogInformation(
                "ZoomRecordingCompleted: duplicate URL skipped for MeetingId {MeetingId}",
                meeting.Id);
            return;
        }

        var recording = new MeetingRecording
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            Status = 1,
            StorageUrl = message.StorageUrl.Trim(),
            ContentType = message.ContentType,
            DurationSeconds = message.DurationSeconds,
            SizeBytes = message.SizeBytes
        };

        await _unitOfWork.MeetingRecordings.AddAsync(recording);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ZoomRecordingCompleted: saved MeetingRecording {RecordingId} for MeetingId {MeetingId}",
            recording.Id,
            meeting.Id);
    }
}
