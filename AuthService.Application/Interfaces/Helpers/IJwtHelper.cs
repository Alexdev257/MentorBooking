using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Helpers
{
    public interface IJwtHelper
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        bool IsTokenValid(string token);
        DateTime ConvertUnixTimeToDateTime(long utcExpiredDate);
        (bool, string?) ValidateToken(string accessToken);
    }
}
