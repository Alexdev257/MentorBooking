using BookingService.Application.Interfaces.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookingService.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserInfoResponse?> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/users/{userId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<UserInfoResponse>>(JsonOptions, cancellationToken);
            if (wrapper is { IsSuccess: true, Data: not null })
                return wrapper.Data;

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserService] Failed to get user info for {userId}: {ex.Message}");
            return null;
        }
    }

    private class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
