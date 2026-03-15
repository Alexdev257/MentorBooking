using BookingService.Application.DTOs.Response;
using BookingService.Domain.Entities;

namespace BookingService.Application.Interfaces.Services;

/// <summary>
/// Integrates with Zoom API to create meetings when a booking is accepted.
/// </summary>
public interface IZoomService
{
    /// <summary>
    /// Creates a Zoom meeting for the given booking.
    /// </summary>
    /// <param name="booking">The confirmed booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (Zoom Meeting Id, Zoom Meeting Join URL, Zoom Meeting Start/Host URL).</returns>
    Task<(string? MeetingId, string? JoinUrl, string? StartUrl)> CreateMeetingAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<string> GetAccessToken(CancellationToken cancellationToken);
    Task<string?> AddRegistrantAsync(string meetingId, string email, string? firstName, string? lastName, CancellationToken ct = default);
    Task<List<ZoomParticipantReport>> GetAttendanceReportAsync(string meetingId, CancellationToken ct = default);
}
