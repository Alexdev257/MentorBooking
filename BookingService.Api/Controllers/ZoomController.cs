using BookingService.Application.DTOs.Request;
using BookingService.Application.DTOs.Response;
using BookingService.Application.Interfaces.Repositories;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public ZoomController(IBookingUnitOfWork unitOfWork, IConfiguration configuration, ILogger<ZoomController> logger, IZoomService zoomService)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _zoomService = zoomService;
    }

    [HttpPost("webhooks")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhooks([FromBody] ZoomWebhookRequest request)
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

        // 2. Extract Meeting Id
        var meetingId = request.Payload.Object?.Id;
        if (string.IsNullOrEmpty(meetingId)) return Ok();

        // 3. Handle Events
        switch (request.Event)
        {
            case "meeting.started":
                await HandleMeetingStarted(meetingId);
                break;

            case "meeting.ended":
                await HandleMeetingEnded(meetingId);
                break;

            case "recording.completed":
                await HandleRecordingCompleted(request);
                break;
        }

        return Ok();
    }

    private async Task HandleMeetingStarted(string meetingId)
    {
        var booking = await _unitOfWork.Bookings.FindAsync(b => b.GoogleEventId == meetingId).FirstOrDefaultAsync();
        if (booking != null)
        {
            _logger.LogInformation("Meeting started for booking {BookingId}", booking.Id);
            // Optional: Update status to "Ongoing" if added to Enum
        }
    }

    private async Task HandleMeetingEnded(string meetingId)
    {
        var booking = await _unitOfWork.Bookings.FindAsync(b => b.GoogleEventId == meetingId).FirstOrDefaultAsync();
        if (booking != null)
        {
            _logger.LogInformation("Meeting ended for booking {BookingId}", booking.Id);
            booking.Status = (int)BookingStatusEnum.Completed;
            _unitOfWork.Bookings.UpdateAsync(booking);
            
            // Fetch Attendance Report
            var attendanceReport = await _zoomService.GetAttendanceReportAsync(meetingId);
            if (attendanceReport.Any())
            {
                var participants = await _unitOfWork.BookingParticipants.FindAsync(p => p.BookingId == booking.Id).ToListAsync();
                foreach (var participant in participants)
                {
                    // Find participant in Zoom report by email
                    var zoomParticipant = attendanceReport.FirstOrDefault(zp => zp.UserEmail?.ToLower() == participant.Email.ToLower());
                    if (zoomParticipant != null)
                    {
                        // Update participant info if we had fields for it, e.g., ActualDuration
                        _logger.LogInformation("Participant {Email} attended for {Duration} seconds", participant.Email, zoomParticipant.Duration);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }

    private async Task HandleRecordingCompleted(ZoomWebhookRequest request)
    {
        var meetingId = request.Payload.Object?.Id;
        var recordings = request.Payload.Object?.RecordingFiles;
        
        if (recordings == null || recordings.Count == 0) return;

        // Find the video file (MP4)
        var videoFile = recordings.FirstOrDefault(f => f.FileType == "MP4");
        if (videoFile == null) return;

        var booking = await _unitOfWork.Bookings.FindAsync(b => b.GoogleEventId == meetingId).FirstOrDefaultAsync();
        if (booking != null)
        {
            _logger.LogInformation("Recording completed for booking {BookingId}. Link: {Link}", booking.Id, videoFile.PlayUrl);
            
            // Store recording link
            // For now, we reuse Notes or MeetingLink if needed, but ideally a new field
            booking.Notes += $"\n[Zoom Recording]: {videoFile.PlayUrl}";
            _unitOfWork.Bookings.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();
            
            // TODO: Notify mentees about recording
        }
    }

    private string HMACSHA256Hash(string plainToken, string secretToken)
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
