using BookingService.Domain.Entities;

namespace BookingService.Application.Interfaces.Services;

/// <summary>
/// Integrates with Google Calendar API: create events with Google Meet link when a booking is accepted.
/// </summary>
public interface IGoogleCalendarService
{
    /// <summary>
    /// Creates a Google Calendar event with Google Meet link for the given booking.
    /// </summary>
    /// <param name="booking">The confirmed booking (ScheduleStart, ScheduleEnd, Topic, Notes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (Google Event Id, Meet/Hangout link). Returns (null, null) if integration is disabled or fails.</returns>
    Task<(string? EventId, string? MeetLink)> CreateEventWithMeetAsync(Booking booking, CancellationToken cancellationToken = default);
}
