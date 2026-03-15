using System.Text.Json.Serialization;

namespace BookingService.Application.DTOs.Response;

public class ZoomAttendanceReportResponse
{
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }

    [JsonPropertyName("participants")]
    public List<ZoomParticipantReport>? Participants { get; set; }
}

public class ZoomParticipantReport
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("user_email")]
    public string? UserEmail { get; set; }

    [JsonPropertyName("join_time")]
    public DateTime JoinTime { get; set; }

    [JsonPropertyName("leave_time")]
    public DateTime LeaveTime { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; } // in seconds
}
