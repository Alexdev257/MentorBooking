namespace Shared.Contracts.Events;

/// <summary>
/// Published by BookingService when Zoom webhooks report meeting lifecycle.
/// MeetingService updates meeting status: 1 = On-going, 2 = Finished.
/// </summary>
public record ZoomMeetingLifecycleEvent(Guid BookingId, int MeetingStatus) : IntegrationEvent;
