using AuthService.Application.DTOs.Response.Auth;
using MediatR;
using SharedContracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedContracts.Common.Wrappers;

namespace AuthService.Application.CQRS.Command.Auth
{
    public class LoginCommand : IRequest<LoginResponse>, IValidatable<LoginResponse>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public Task<LoginResponse> ValidateAsync()
        {
            var response = new LoginResponse();
            if (string.IsNullOrEmpty(Email))
                response.ListErrors.Add(new Errors
                {
                    Field = "Email",
                    Detail = "Email is null or empty"
                });
            if (string.IsNullOrEmpty(Password))
                response.ListErrors.Add(new Errors
                {
                    Field = "Password",
                    Detail = "Password is null or empty"
                });
            if (response.ListErrors.Count > 0) response.IsSuccess = false;
            return Task.FromResult(response);
        }
    }
}
