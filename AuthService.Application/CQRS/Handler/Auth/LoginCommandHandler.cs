using AuthService.Application.CQRS.Command.Auth;
using AuthService.Application.DTOs.Response.Auth;
using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using MediatR;
using SharedContracts.Interfaces;
using SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.CQRS.Handler.Auth
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IAuthUnitOfWork _unitOfWork;
        private readonly IBcryptHelper _bcryptHelper;
        private readonly IJwtHelper _jwtHelper;
        private readonly ICacheService _cacheService;
        public LoginCommandHandler(IAuthUnitOfWork unitOfWork, IBcryptHelper bcryptHelper, IJwtHelper jwtHelper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _bcryptHelper = bcryptHelper;
            _jwtHelper = jwtHelper;
            _cacheService = cacheService;
        }
        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = _unitOfWork.Users.GetAllAsync()
                .FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    Message = "Invalid email",
                };
            }
            var isPasswordValid = _bcryptHelper.VerifyPassword(request.Password, user.Password);
            if (!isPasswordValid)
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    Message = "Invalid password",
                };
            }

            var accessToken = _jwtHelper.GenerateAccessToken(user);
            var refreshToken = _jwtHelper.GenerateRefreshToken();
            await _cacheService.SetAsync($"RT_{user.Id}", refreshToken, TimeSpan.FromDays(7), cancellationToken);
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
}
