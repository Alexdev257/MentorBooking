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
    /// Zoom webhooks. Subscribe in Zoom App to: endpoint.url_validation, meeting.started,
    /// meeting.participant_joined, meeting.ended, recording.completed.
    /// </summary>
    [HttpPost("webhooks")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhooks([FromBody] ZoomWebhookRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received Zoom Webhook: {Event}", request.Event);

        // 1. Handle Zoom CRC (Challenge Response Check)
        if (request.Event == "endpoint.url_validation")
        {
            var plainToken = request.Payload.PlainToken;
            var secretToken = _configuration["Zoom:SecretToken"];

            if (string.IsNullOrEmpty(plainToken) || string.IsNullOrEmpty(secretToken))
            {
                return BadRequest("Missing token for validation");
            }

            var hash = HMACSHA256Hash(plainToken, secretToken);
            return Ok(new ZoomCrcResponse
            {
                PlainToken = plainToken,
                EncryptedToken = hash
            });
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
                await HandleMeetingEndedAsync(meetingId, cancellationToken);
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

    private async Task HandleMeetingEndedAsync(string zoomMeetingId, CancellationToken cancellationToken)
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

        var attendanceReport = await _zoomService.GetAttendanceReportAsync(zoomMeetingId);
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

        if (recordings == null || recordings.Count == 0) return;

        var videoFile = recordings.FirstOrDefault(f => f.FileType == "MP4");
        if (videoFile == null) return;

        var booking = await _unitOfWork.Bookings.FindAsync(b => b.GoogleEventId == meetingId).FirstOrDefaultAsync(cancellationToken);
        if (booking == null) return;

        _logger.LogInformation("Recording completed for booking {BookingId}. Link: {Link}", booking.Id, videoFile.PlayUrl);

        booking.Notes += $"\n[Zoom Recording]: {videoFile.PlayUrl}";
        _unitOfWork.Bookings.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
