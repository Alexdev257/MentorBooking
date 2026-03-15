using System;

namespace Shared.Contracts.Events
{
    public record SendZoomInviteEvent : IntegrationEvent
    {
        public string ToEmail { get; init; } = string.Empty;
        public string MenteeName { get; init; } = string.Empty;
        public string MentorName { get; init; } = string.Empty;
        public string Topic { get; init; } = string.Empty;
        public string JoinUrl { get; init; } = string.Empty;
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }

        public SendZoomInviteEvent(string toEmail, string menteeName, string mentorName, string topic, string joinUrl, DateTime startTime, DateTime endTime)
        {
            ToEmail = toEmail;
            MenteeName = menteeName;
            MentorName = mentorName;
            Topic = topic;
            JoinUrl = joinUrl;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
