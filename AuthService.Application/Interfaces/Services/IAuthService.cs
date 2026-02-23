using AuthService.Application.DTOs.Request.Auth;
using AuthService.Application.DTOs.Response.Auth;

namespace AuthService.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
