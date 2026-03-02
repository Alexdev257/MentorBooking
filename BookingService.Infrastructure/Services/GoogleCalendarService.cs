using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Services;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private const string ConfigSection = "GoogleCalendar";

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(string? EventId, string? MeetLink)> CreateEventWithMeetAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var enabled = _configuration.GetValue<bool>($"{ConfigSection}:Enabled");
        if (!enabled)
        {
            _logger.LogDebug("Google Calendar integration is disabled.");
            return (null, null);
        }

        var calendarId = _configuration[$"{ConfigSection}:CalendarId"] ?? "primary";
        var jsonPath = _configuration[$"{ConfigSection}:ServiceAccountJsonPath"];
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            _logger.LogWarning("GoogleCalendar:ServiceAccountJsonPath is not set. Skipping calendar event creation.");
            return (null, null);
        }

        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("Service account JSON file not found at {Path}. Skipping calendar event creation.", jsonPath);
            return (null, null);
        }

        try
        {
            using var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var credential = await GoogleCredential.FromStreamAsync(stream, cancellationToken);
            credential = credential.CreateScoped(CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents);

            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MentorBooking"
            };
            using var service = new CalendarService(initializer);

            var ev = new Event
            {
                Summary = string.IsNullOrWhiteSpace(booking.Topic) ? "Mentor session" : booking.Topic,
                Description = booking.Notes ?? $"Booking {booking.Id}. Mentor session.",
                Start = new EventDateTime
                {
                    DateTime = booking.ScheduleStart.Kind == DateTimeKind.Utc ? booking.ScheduleStart : DateTime.SpecifyKind(booking.ScheduleStart, DateTimeKind.Utc),
                    TimeZone = _configuration[$"{ConfigSection}:TimeZone"] ?? "UTC"
                },
                End = new EventDateTime
                {
                    DateTime = booking.ScheduleEnd.Kind == DateTimeKind.Utc ? booking.ScheduleEnd : DateTime.SpecifyKind(booking.ScheduleEnd, DateTimeKind.Utc),
                    TimeZone = _configuration[$"{ConfigSection}:TimeZone"] ?? "UTC"
                },
                ConferenceData = new ConferenceData
                {
                    CreateRequest = new CreateConferenceRequest
                    {
                        RequestId = Guid.NewGuid().ToString("N"),
                        ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" }
                    }
                }
            };

            var insertRequest = service.Events.Insert(ev, calendarId);
            insertRequest.ConferenceDataVersion = 1;
            var created = await insertRequest.ExecuteAsync(cancellationToken);

            var meetLink = created.HangoutLink ?? created.ConferenceData?.EntryPoints?.FirstOrDefault(e => e.EntryPointType == "video")?.Uri;
            _logger.LogInformation("Created Google Calendar event {EventId} for booking {BookingId}. Meet: {MeetLink}", created.Id, booking.Id, meetLink);
            return (created.Id, meetLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Calendar event for booking {BookingId}.", booking.Id);
            return (null, null);
        }
    }
}
