using AuthService.Application.DTOs.Request.Auth;
using AuthService.Application.DTOs.Response.Auth;
using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IBcryptHelper _bcryptHelper;
    private readonly IJwtHelper _jwtHelper;

    public AuthService(IAuthUnitOfWork unitOfWork, IBcryptHelper bcryptHelper, IJwtHelper jwtHelper)
    {
        _unitOfWork = unitOfWork;
        _bcryptHelper = bcryptHelper;
        _jwtHelper = jwtHelper;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users
            .FindAsync(u => u.Email == request.Email)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return new LoginResponse { IsSuccess = false, Message = "Invalid email or password" };

        if (!_bcryptHelper.VerifyPassword(request.Password, user.Password))
            return new LoginResponse { IsSuccess = false, Message = "Invalid email or password" };

        var accessToken = _jwtHelper.GenerateAccessToken(user);
        var refreshToken = _jwtHelper.GenerateRefreshToken();

        return new LoginResponse
        {
            IsSuccess = true,
            Message = "Login successfully",
            Data = new TokenDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            }
        };
    }
}
