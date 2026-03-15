using System;

namespace Shared.Contracts.Events
{
    public record SendZoomHostInviteEvent : IntegrationEvent
    {
        public string MentorEmail { get; init; } = string.Empty;
        public string MentorName { get; init; } = string.Empty;
        public string Topic { get; init; } = string.Empty;
        public string HostUrl { get; init; } = string.Empty; // This is the start_url
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }

        public SendZoomHostInviteEvent(string mentorEmail, string mentorName, string topic, string hostUrl, DateTime startTime, DateTime endTime)
        {
            MentorEmail = mentorEmail;
            MentorName = mentorName;
            Topic = topic;
            HostUrl = hostUrl;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
