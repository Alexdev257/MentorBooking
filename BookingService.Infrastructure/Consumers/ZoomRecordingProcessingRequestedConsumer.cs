using BookingService.Application.Interfaces.Repositories;
using BookingService.Application.Interfaces.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Contracts.Interfaces;

namespace BookingService.Infrastructure.Consumers;

public class ZoomRecordingProcessingRequestedConsumer : IConsumer<ZoomRecordingProcessingRequestedEvent>
{
    private readonly IMeetingRecordingCloudMirrorService _mirror;
    private readonly IZoomRecordingAiUploadService _aiUpload;
    private readonly IBookingUnitOfWork _unitOfWork;
    private readonly IMessageProducer _producer;
    private readonly ILogger<ZoomRecordingProcessingRequestedConsumer> _logger;

    public ZoomRecordingProcessingRequestedConsumer(
        IMeetingRecordingCloudMirrorService mirror,
        IZoomRecordingAiUploadService aiUpload,
        IBookingUnitOfWork unitOfWork,
        IMessageProducer producer,
        ILogger<ZoomRecordingProcessingRequestedConsumer> logger)
    {
        _mirror = mirror;
        _aiUpload = aiUpload;
        _unitOfWork = unitOfWork;
        _producer = producer;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ZoomRecordingProcessingRequestedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "ZoomRecordingProcessingRequested: BookingId={BookingId} MeetingId={MeetingId} Url={Url} ContentType={Ct} SizeBytes={Size}",
            msg.BookingId,
            msg.MeetingId,
            RedactUrl(msg.RecordingDownloadUrl),
            msg.ContentType,
            msg.SizeBytes);

        var firebaseUrl = await _mirror.TryMirrorToFirebaseAsync(
            msg.BookingId,
            msg.MeetingId,
            msg.RecordingDownloadUrl,
            msg.ZoomDownloadToken,
            "video",
            msg.Extension,
            msg.ContentType,
            ct);

        var (aiOk, aiTranscriptId) = await _aiUpload.UploadRecordingToAiAsync(
            msg.BookingId,
            msg.MeetingId,
            msg.RecordingDownloadUrl,
            msg.ZoomDownloadToken,
            $"{msg.MeetingId}.mp4",
            msg.ContentType,
            ct);

        if (!string.IsNullOrWhiteSpace(firebaseUrl))
        {
            await _producer.PublishAsync(
                new ZoomRecordingCompletedEvent(
                    msg.BookingId,
                    firebaseUrl.Trim(),
                    msg.ContentType,
                    msg.DurationSeconds,
                    msg.SizeBytes,
                    null,
                    null,
                    null),
                ct);
        }

        var booking = await _unitOfWork.Bookings
            .FindAsync(b => b.Id == msg.BookingId)
            .FirstOrDefaultAsync(ct);

        if (booking != null)
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(firebaseUrl))
                lines.Add($"[Meeting Recording]: {firebaseUrl.Trim()}");
            if (aiOk)
            {
                lines.Add("[AI Recording Upload]: success");
                if (!string.IsNullOrWhiteSpace(aiTranscriptId))
                {
                    lines.Add($"[AI Audio Transcript Id]: {aiTranscriptId}");
                }
            }

            if (lines.Count > 0)
            {
                var currentNotes = booking.Notes ?? string.Empty;
                booking.Notes = $"{currentNotes}\n{string.Join("\n", lines)}".Trim();
                _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }

        _logger.LogInformation(
            "===== ZOOM RECORDING PROCESSING FULLY COMPLETED =====\nBookingId: {BookingId}\nFirebase Uploaded: {Fb}\nAI Uploaded: {Ai}\nTranscriptId: {TranscriptId}\n=====================================================",
            msg.BookingId,
            firebaseUrl != null ? "YES" : "NO",
            aiOk ? "YES" : "NO",
            string.IsNullOrWhiteSpace(aiTranscriptId) ? "N/A" : aiTranscriptId);
    }

    private static string RedactUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;
        var q = url.IndexOf('?');
        return q >= 0 ? $"{url[..q]}?*" : url;
    }
}

