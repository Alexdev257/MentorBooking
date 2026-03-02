using AuthService.Application.DTOs.Request.Auth;
using AuthService.Application.DTOs.Response.Auth;
using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Interfaces;

namespace AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IBcryptHelper _bcryptHelper;
    private readonly IJwtHelper _jwtHelper;
    private readonly ICacheService _cacheService;

    public AuthService(IAuthUnitOfWork unitOfWork, IBcryptHelper bcryptHelper, IJwtHelper jwtHelper, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _bcryptHelper = bcryptHelper;
        _jwtHelper = jwtHelper;
        _cacheService = cacheService;
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
        await _cacheService.SetAsync<string>($"RT_{user.Id}", refreshToken, TimeSpan.FromDays(7), cancellationToken);

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

    public async Task<LogoutResponse> LogoutAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await _cacheService.GetAsync<string>($"RT_{id}");
        if (token == null)
        {
            return new LogoutResponse
            {
                IsSuccess = false,
                Message = "User is already logged out."
            };
        }
        else
        {
            await _cacheService.RemoveAsync($"RT_{id}");
            return new LogoutResponse
            {
                IsSuccess = true,
                Message = "User logged out successfully."
            };
        }
    }

    public async Task<LoginResponse> RefreshAsync(Guid id, RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var rs = _jwtHelper.ValidateToken(request.AccessToken);
        if (!rs.Item1)
            return new LoginResponse
            {
                IsSuccess = false,
                Message = rs.Item2
            };
        var refreshToken = await _cacheService.GetAsync<string>($"RT_{id}");
        if (string.IsNullOrEmpty(refreshToken))
            return new LoginResponse
            {
                IsSuccess = false,
                Message = "RefreshToken is used or expired!"
            };

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        var newAccessToken = _jwtHelper.GenerateAccessToken(user!);
        var newRefreshToken = _jwtHelper.GenerateRefreshToken();
        await _cacheService.RemoveAsync($"RT_{id}");
        await _cacheService.SetAsync($"RT_{id}", newRefreshToken);
        return new LoginResponse
        {
            IsSuccess = true,
            Message = "Refresh successfully",
            Data = new TokenDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            }
        };
    }
}
