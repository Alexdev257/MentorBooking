using AuthService.Application.CQRS.Command.Auth;
using AuthService.Application.DTOs.Response.Auth;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using MediatR;
using SharedContracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.CQRS.Handler.Auth
{
    public class VerifyRegisterCommandHandler : IRequestHandler<VerifyRegisterCommand, VerifyRegisterResponse>
    {
        private readonly IAuthUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        public VerifyRegisterCommandHandler(IAuthUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }
        public async Task<VerifyRegisterResponse> Handle(VerifyRegisterCommand request, CancellationToken cancellationToken)
        {
            var user = await _cacheService.GetAsync<User>($"REG_{request.Email}");
            var otp = await _cacheService.GetAsync<string>($"OTP_REG_{request.Email}");
            if(user == null ||  otp == null)
            {
                return new VerifyRegisterResponse
                {
                    IsSuccess = false,
                    Message = "OTP is expired"
                };
            }
            if(request.Otp != otp)
            {
                return new VerifyRegisterResponse
                {
                    IsSuccess = false,
                    Message = "OTP is incorrect"
                };
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CommitTransactionAsync();
                return new VerifyRegisterResponse
                {
                    IsSuccess = true,
                    Message = "Register successfully",
                    Data = new VerifyOtpDTO
                    {
                        Email = user.Email,
                        Otp = otp,
                    }
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
