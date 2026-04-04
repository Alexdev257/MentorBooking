namespace BookingService.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserInfoResponse?> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class UserInfoResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
