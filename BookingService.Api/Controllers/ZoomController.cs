using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Repositories;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;
using Shared.Contracts.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BookingService.Api.Controllers;

[ApiController]
[Route("api/zoom")]
public class ZoomController : ControllerBase
{
    private readonly IBookingUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZoomController> _logger;
    private readonly IZoomService _zoomService;
    private readonly IMessageProducer _messageProducer;

    public ZoomController(
        IBookingUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<ZoomController> logger,
        IZoomService zoomService,
        IMessageProducer messageProducer)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _zoomService = zoomService;
        _messageProducer = messageProducer;
    }

    /// <summary>
    /// Zoom Event Subscriptions — POST <c>/api/zoom/wh</c>.
    /// In Zoom Marketplace: set "Event notification endpoint URL" to <c>https://&lt;host&gt;/api/zoom/wh</c> (must match gateway).
    /// Required config: <c>Zoom:SecretToken</c> (Verification Token from the Zoom app) for URL validation.
    /// Events: endpoint.url_validation, meeting.started, meeting.participant_joined, meeting.ended, recording.completed.
    /// </summary>
    [HttpPost("wh")]
    [AllowAnonymous]
    public Task<IActionResult> HandleWebhooks([FromBody] ZoomWebhookRequest? request, CancellationToken cancellationToken)
        => HandleWebhooksCore(request, cancellationToken);

    /// <summary>
    /// Lets you verify the public URL is routed (browser or curl). Zoom validation always uses POST with JSON.
    /// </summary>
    [HttpGet("wh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ZoomWebhookEndpointInfo()
    {
        return Ok(new
        {
            ok = true,
            message = "Zoom Event Subscription endpoint. Use POST with JSON. URL validation sends event endpoint.url_validation.",
            path = "/api/zoom/wh"
        });
    }

    private async Task<IActionResult> HandleWebhooksCore(ZoomWebhookRequest? request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            _logger.LogWarning("Zoom webhook: empty body");
            return BadRequest(new { message = "Expected JSON body." });
        }

        _logger.LogInformation("Received Zoom Webhook: {Event}", request.Event);

        // 1. Zoom URL validation (Challenge Response Check) — required for "Validate" in developer portal
        if (string.Equals(request.Event, "endpoint.url_validation", StringComparison.OrdinalIgnoreCase))
        {
            var plainToken = request.Payload?.PlainToken;
            var secretToken = _configuration["Zoom:SecretToken"];

            if (string.IsNullOrEmpty(plainToken))
            {
                _logger.LogWarning("Zoom url_validation: missing payload.plainToken");
                return BadRequest(new { message = "Missing payload.plainToken" });
            }

            if (string.IsNullOrEmpty(secretToken))
            {
                _logger.LogError("Zoom url_validation: Zoom:SecretToken is not configured (set Verification Token from Zoom app).");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Server missing Zoom:SecretToken. Copy Secret Token from Zoom app to configuration." });
            }

            var hash = HMACSHA256Hash(plainToken, secretToken);
            return Ok(new ZoomCrcResponse
            {
                PlainToken = plainToken,
                EncryptedToken = hash
            });
        }

        if (request.Payload == null)
        {
            _logger.LogWarning("Zoom webhook {Event}: null payload", request.Event);
            return Ok();
        }

        // 2. Extract Zoom meeting id (numeric string stored in Booking.GoogleEventId)
        var meetingId = request.Payload.Object?.Id;
        if (string.IsNullOrEmpty(meetingId))
        {
            _logger.LogInformation("Zoom webhook {Event}: no payload.object.id, skipping.", request.Event);
            return Ok();
        }

        // 3. Handle Events
        switch (request.Event)
        {
            case "meeting.started":
                await HandleMeetingStartedAsync(meetingId, cancellationToken);
                break;

            case "meeting.participant_joined":
                await HandleMeetingStartedAsync(meetingId, cancellationToken);
                break;

            case "meeting.ended":
                await HandleMeetingEndedAsync(meetingId, request.Payload.Object?.Uuid, cancellationToken);
                break;

            case "recording.completed":
                await HandleRecordingCompleted(request, cancellationToken);
                break;
        }

        return Ok();
    }

    private async Task HandleMeetingStartedAsync(string zoomMeetingId, CancellationToken cancellationToken)
    {
        var booking = await _unitOfWork.Bookings.FindAsync(b => b.GoogleEventId == zoomMeetingId).FirstOrDefaultAsync(cancellationToken);
        if (booking == null)
        {
            _logger.LogWarning("No booking for Zoom meeting id {ZoomMeetingId}", zoomMeetingId);
            return;
        }

        _logger.LogInformation("Zoom meeting active (started/joined) for booking {BookingId}", booking.Id);
        await _messageProducer.PublishAsync(new ZoomMeetingLifecycleEvent(booking.Id, 1), cancellationToken);
    }

    private async Task HandleMeetingEndedAsync(string zoomMeetingId, string? zoomMeetingUuid, CancellationToken cancellationToken)
    {
        var booking = await _unitOfWork.Bookings.FindAsync(b => b.GoogleEventId == zoomMeetingId).FirstOrDefaultAsync(cancellationToken);
        if (booking == null)
        {
            _logger.LogWarning("No booking for Zoom meeting id {ZoomMeetingId}", zoomMeetingId);
            return;
        }

        _logger.LogInformation("Meeting ended for booking {BookingId}", booking.Id);
        booking.Status = (int)BookingStatusEnum.Completed;
        _unitOfWork.Bookings.UpdateAsync(booking);

        var attendanceReport = await _zoomService.GetAttendanceReportAsync(zoomMeetingId, zoomMeetingUuid, cancellationToken);
        if (attendanceReport.Any())
        {
            var participants = await _unitOfWork.BookingParticipants.FindAsync(p => p.BookingId == booking.Id).ToListAsync(cancellationToken);
            foreach (var participant in participants)
            {
                var zoomParticipant = attendanceReport.FirstOrDefault(zp => zp.UserEmail?.ToLower() == participant.Email.ToLower());
                if (zoomParticipant != null)
                {
                    _logger.LogInformation("Participant {Email} attended for {Duration} seconds", participant.Email, zoomParticipant.Duration);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _messageProducer.PublishAsync(new ZoomMeetingLifecycleEvent(booking.Id, 2), cancellationToken);
    }

    private async Task HandleRecordingCompleted(ZoomWebhookRequest request, CancellationToken cancellationToken)
    {
        var meetingId = request.Payload.Object?.Id;
        var recordings = request.Payload.Object?.RecordingFiles;

        if (recordings == null || recordings.Count == 0)
        {
            _logger.LogWarning("Recording completed webhook for meeting {MeetingId} has no recording files", meetingId);
            return;
        }

        var videoFile = GetPreferredZoomRecordingFile(recordings);
        var transcriptFile = GetZoomTranscriptFile(recordings);
        if (videoFile == null && transcriptFile == null)
        {
            _logger.LogWarning("Recording completed webhook for meeting {MeetingId} has no video/audio or transcript file", meetingId);
            return;
        }

        var booking = await _unitOfWork.Bookings.FindAsync(b => b.GoogleEventId == meetingId).FirstOrDefaultAsync(cancellationToken);
        if (booking == null)
        {
            _logger.LogWarning("Recording completed webhook has no matching booking for meeting {MeetingId}", meetingId);
            return;
        }

        var recordingUrl = videoFile != null ? PickZoomPlayOrDownloadUrl(videoFile) : null;
        var transcriptUrl = transcriptFile != null ? PickZoomPlayOrDownloadUrl(transcriptFile) : null;

        if (string.IsNullOrWhiteSpace(recordingUrl) && string.IsNullOrWhiteSpace(transcriptUrl))
        {
            _logger.LogWarning("Recording completed for booking {BookingId} but no play/download URL for video or transcript", booking.Id);
            return;
        }

        var noteLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(recordingUrl))
            noteLines.Add($"[Zoom Recording]: {recordingUrl.Trim()}");
        if (!string.IsNullOrWhiteSpace(transcriptUrl))
            noteLines.Add($"[Zoom Transcript]: {transcriptUrl.Trim()}");

        _logger.LogInformation(
            "Recording completed for booking {BookingId}. Recording: {HasRec} Transcript: {HasTr}",
            booking.Id,
            recordingUrl != null,
            transcriptUrl != null);

        var currentNotes = booking.Notes ?? string.Empty;
        booking.Notes = $"{currentNotes}\n{string.Join("\n", noteLines)}".Trim();
        _unitOfWork.Bookings.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var contentType = videoFile != null ? MapZoomFileTypeToContentType(videoFile.FileType) : null;
        int? durationSeconds = null;
        if (videoFile?.RecordingEnd is { } end && videoFile.RecordingStart is { } start && end > start)
            durationSeconds = (int)(end - start).TotalSeconds;

        var transcriptContentType = transcriptFile != null ? MapZoomFileTypeToContentType(transcriptFile.FileType) : null;

        await _messageProducer.PublishAsync(
            new ZoomRecordingCompletedEvent(
                booking.Id,
                recordingUrl?.Trim(),
                contentType,
                durationSeconds,
                videoFile?.FileSize,
                transcriptUrl?.Trim(),
                transcriptContentType,
                transcriptFile?.FileSize),
            cancellationToken);
    }

    /// <summary>MP4 → M4A → first file that is not transcript/chat/thumbnail sidecar.</summary>
    private static ZoomRecordingFile? GetPreferredZoomRecordingFile(List<ZoomRecordingFile> recordings)
    {
        var mp4 = recordings.FirstOrDefault(f => string.Equals(f.FileType, "MP4", StringComparison.OrdinalIgnoreCase));
        if (mp4 != null)
            return mp4;
        var m4a = recordings.FirstOrDefault(f => string.Equals(f.FileType, "M4A", StringComparison.OrdinalIgnoreCase));
        if (m4a != null)
            return m4a;
        return recordings.FirstOrDefault(f => !IsZoomTranscriptOrSidecarArtifact(f.FileType));
    }

    private static ZoomRecordingFile? GetZoomTranscriptFile(List<ZoomRecordingFile> recordings) =>
        recordings.FirstOrDefault(f => IsZoomNativeTranscriptFile(f.FileType));

    private static bool IsZoomNativeTranscriptFile(string? fileType)
    {
        if (string.IsNullOrWhiteSpace(fileType))
            return false;
        return fileType.Trim().ToUpperInvariant() switch
        {
            "TRANSCRIPT" or "CC" or "AUDIO_TRANSCRIPT" => true,
            _ => false,
        };
    }

    private static bool IsZoomTranscriptOrSidecarArtifact(string? fileType)
    {
        if (string.IsNullOrWhiteSpace(fileType))
            return false;
        return fileType.Trim().ToUpperInvariant() switch
        {
            "TRANSCRIPT" or "CC" or "AUDIO_TRANSCRIPT" or "CHAT" or "CSV" or "THUMBNAIL" => true,
            _ => false,
        };
    }

    private static string? PickZoomPlayOrDownloadUrl(ZoomRecordingFile file) =>
        !string.IsNullOrWhiteSpace(file.PlayUrl) ? file.PlayUrl : file.DownloadUrl;

    private static string? MapZoomFileTypeToContentType(string? fileType)
    {
        if (string.IsNullOrWhiteSpace(fileType))
            return null;
        return fileType.Trim().ToUpperInvariant() switch
        {
            "MP4" => "video/mp4",
            "M4A" => "audio/mp4",
            "MP3" => "audio/mpeg",
            "CHAT" => "text/plain",
            "TRANSCRIPT" or "CC" or "AUDIO_TRANSCRIPT" => "text/vtt",
            _ => null
        };
    }

    private static string HMACSHA256Hash(string plainToken, string secretToken)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretToken);
        var messageBytes = Encoding.UTF8.GetBytes(plainToken);
        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
