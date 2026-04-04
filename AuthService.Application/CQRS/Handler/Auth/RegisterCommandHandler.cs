using AuthService.Application.CQRS.Command.Auth;
using AuthService.Application.DTOs.Response.Auth;
using AuthService.Application.Interfaces;
using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using MediatR;
using Shared.Contracts.Events;
using Shared.Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.CQRS.Handler.Auth
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
    {
        private readonly IAuthUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IBcryptHelper _bcryptHelper;
        private readonly IMessageProducer _messageProducer;
        public RegisterCommandHandler(IAuthUnitOfWork unitOfWork, ICacheService cacheService, IBcryptHelper bcryptHelper, IMessageProducer messageProducer)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _bcryptHelper = bcryptHelper;
            _messageProducer = messageProducer;
        }
        public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var existingEmail = _unitOfWork.Users.GetAllAsync()
                .Any(u => u.Email == request.Email);
            if (existingEmail)
            {
                return new RegisterResponse
                {
                    IsSuccess = false,
                    Message = "Email already exists",
                };
            }

            if (!string.IsNullOrEmpty(request.Phone))
            {
                var existingPhone = _unitOfWork.Users.GetAllAsync().Any();
                //.Any(u => u.Phone == request.Phone); // User entity does not have Phone field
                if (existingPhone)
                {
                    return new RegisterResponse
                    {
                        IsSuccess = false,
                        Message = "Email already exists",
                    };
                }
            }

            var hashPassword = _bcryptHelper.HashPassword(request.Password);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Password = hashPassword,
                Fullname = request.FullName,
                AvatarUrl = request.AvatarUrl ?? string.Empty,
                Role = (int)AuthService.Domain.Enum.RoleNameEnum.Student,
            };

            //await _cacheService.SetAsync<User>($"REG_{user.Email}", user, TimeSpan.FromMinutes(15), cancellationToken);
            //var otp = OtpHelper.GenerateOtp();
            //await _cacheService.SetAsync<string>($"OTP_REG_{user.Email}", otp, TimeSpan.FromMinutes(5), cancellationToken);
            //await _messageProducer.PublishAsync<SendOtpRegisterEvent>(new SendOtpRegisterEvent(request.Email, otp), cancellationToken);
            //return new RegisterResponse
            //{
            //    IsSuccess = true,
            //    Message = "Register successfully",
            //};
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CommitTransactionAsync();
                return new RegisterResponse
                {
                    IsSuccess = true,
                    Message = "Register successfully",
                    // UserId = user.Id // Trả về ID nếu cần
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

        }
    }
}
