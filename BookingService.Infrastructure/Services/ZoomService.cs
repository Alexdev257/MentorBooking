using BookingService.Application.DTOs.Response;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BookingService.Infrastructure.Services;

public class ZoomService : IZoomService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZoomService> _logger;
    private readonly HttpClient _httpClient;
    private const string ConfigSection = "Zoom";

    public ZoomService(IConfiguration configuration, ILogger<ZoomService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<(string? MeetingId, string? JoinUrl, string? StartUrl)> CreateMeetingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        // // Mock implementation for now as per instructions
        // _logger.LogInformation("Creating mock Zoom meeting for booking {BookingId}", booking.Id);
        
        // // In a real implementation, you would:
        // // 1. Get OAuth token using AccountId, ClientId, ClientSecret
        // // 2. Call Zoom API: POST /users/me/meetings
        
        // var mockMeetingId = Guid.NewGuid().ToString("N");
        // var mockMeetingUrl = $"https://zoom.us/j/{mockMeetingId}";

        // return await Task.FromResult((mockMeetingId, mockMeetingUrl));
        
        // /* 
        // // Example of real implementation logic:
        // var zoomUser = "me"; 
        // var requestBody = new
        // {
        //     topic = booking.Topic ?? "Mentor Session",
        //     type = 2, // Scheduled meeting
        //     start_time = booking.ScheduleStart.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        //     duration = (int)(booking.ScheduleEnd - booking.ScheduleStart).TotalMinutes,
        //     timezone = "Asia/Ho_Chi_Minh",
        //     settings = new
        //     {
        //         host_video = true,
        //         participant_video = true,
        //         join_before_host = true,
        //         mute_upon_entry = true
        //     }
        // };

        // var response = await _httpClient.PostAsJsonAsync($"users/{zoomUser}/meetings", requestBody, cancellationToken);
        // if (response.IsSuccessStatusCode)
        // {
        //     var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        //     var id = content.GetProperty("id").ToString();
        //     var joinUrl = content.GetProperty("join_url").GetString();
        //     return (id, joinUrl);
        // }
        
        // _logger.LogError("Failed to create Zoom meeting. Status: {Status}", response.StatusCode);
        // return (null, null);
        // */

        try
        {
            var token = await GetAccessToken(cancellationToken);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // var requestBody = new
            // {
            //     topic = booking.Topic ?? "Mentor Session",
            //     type = 2,
            //     start_time = booking.ScheduleStart.ToString("yyyy-MM-ddTHH:mm:ss"),
            //     duration = (int)(booking.ScheduleEnd - booking.ScheduleStart).TotalMinutes,
            //     timezone = "Asia/Ho_Chi_Minh",
            //     settings = new
            //     {
            //         host_video = true,
            //         participant_video = true,
            //         join_before_host = true,
            //         mute_upon_entry = true,
            //         auto_recording = "cloud",
            //         recording_encryption = true,
            //         approval_type = 0,
            //         registration_type = 1
            //     }
            // };

            var requestBody = new
            {
                topic = booking.Topic ?? "Mentor Session",
                type = 2,
                start_time = booking.ScheduleStart
                    .ToUniversalTime()
                    .ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = (int)(booking.ScheduleEnd - booking.ScheduleStart).TotalMinutes,

                settings = new
                {
                    host_video = true,
                    participant_video = true,
                    join_before_host = false, // 🔥 FIX
                    mute_upon_entry = true,
                    auto_recording = "cloud",
                    approval_type = 0,
                    registration_type = 1
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                "users/me/meetings",
                requestBody,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zoom meeting creation failed: {Status}",
                    response.StatusCode);
                return (null, null, null);
            }

            var meeting = await response.Content
                .ReadFromJsonAsync<ZoomMeetingResponse>(cancellationToken: cancellationToken);

            return (meeting?.id.ToString(), meeting?.join_url, meeting?.start_url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Zoom meeting");
            return (null, null, null);
        }
    }

    public async Task<string> GetAccessToken(CancellationToken cancellationToken)
    {
        var accountId = _configuration["Zoom:AccountId"];
        var clientId = _configuration["Zoom:ClientId"];
        var clientSecret = _configuration["Zoom:ClientSecret"];

        var auth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
        );

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={accountId}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", auth);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<ZoomTokenResponse>(cancellationToken: cancellationToken);

        Console.WriteLine($"Zoom AccessToken:{tokenResponse.access_token}");
        return tokenResponse!.access_token;
    }

    public async Task<string?> AddRegistrantAsync(string meetingId, string email, string? firstName, string? lastName, CancellationToken ct = default)
    {
        try
        {
            var token = await GetAccessToken(ct);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var requestBody = new
            {
                email = email,
                first_name = firstName ?? "Mentee",
                last_name = lastName ?? "Guest"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"meetings/{meetingId}/registrants",
                requestBody,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Zoom registrant addition failed: {Status}. Error: {Error}",
                    response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return content.GetProperty("join_url").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding Zoom registrant");
            return null;
        }
    }

    public async Task<List<ZoomParticipantReport>> GetAttendanceReportAsync(string meetingId, CancellationToken ct = default)
    {
        try
        {
            var token = await GetAccessToken(ct);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"report/meetings/{meetingId}/participants?page_size=30", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get attendance report: {Status}", response.StatusCode);
                return new List<ZoomParticipantReport>();
            }

            var report = await response.Content.ReadFromJsonAsync<ZoomAttendanceReportResponse>(cancellationToken: ct);
            return report?.Participants ?? new List<ZoomParticipantReport>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance report");
            return new List<ZoomParticipantReport>();
        }
    }
}
