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
    public class RegisterCommand : IRequest<RegisterResponse>/*, IValidatable<RegisterResponse>*/
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; }
        public string? AvatarUrl { get; set; }
        public int? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }

        //public Task<RegisterResponse> ValidateAsync()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
