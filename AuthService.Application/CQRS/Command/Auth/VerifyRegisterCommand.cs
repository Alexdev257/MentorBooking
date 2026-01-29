using AuthService.Application.DTOs.Response.Auth;
using MediatR;
using SharedContracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.CQRS.Command.Auth
{
    public class VerifyRegisterCommand : IRequest<VerifyRegisterResponse>, IValidatable<VerifyRegisterResponse>
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public Task<VerifyRegisterResponse> ValidateAsync()
        {
            var response = new VerifyRegisterResponse();
            if (string.IsNullOrWhiteSpace(Email))
            {
                response.ListErrors.Add(new SharedContracts.Common.Wrappers.Errors
                {
                    Field = "Email",
                    Detail = "Email is not empty",
                });
            }
            if (string.IsNullOrWhiteSpace(Otp))
            {
                response.ListErrors.Add(new SharedContracts.Common.Wrappers.Errors
                {
                    Field = "OTP",
                    Detail = "OTP is not null or empty"
                });
            }
            if(!int.TryParse(Otp, out var _))
            {
                response.ListErrors.Add(new SharedContracts.Common.Wrappers.Errors
                {
                    Field = "OTP",
                    Detail = "OTP must be digit"
                });
            }
            if(Otp.Length != 6)
            {
                response.ListErrors.Add(new SharedContracts.Common.Wrappers.Errors
                {
                    Field = "OTP",
                    Detail = "OTP must be 6 digit"
                });
            }
            if (response.ListErrors.Count > 0) response.IsSuccess = false;
            return Task.FromResult(response);
        }
    }
}
