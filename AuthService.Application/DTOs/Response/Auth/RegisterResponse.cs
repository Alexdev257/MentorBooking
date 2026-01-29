using AuthService.Domain.Entities;
using AuthService.Domain.Enum;
using SharedContracts.Common.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs.Response.Auth
{
    public class RegisterResponse : CommonResponse<RegisterDTO> { }

    public class RegisterDTO
    {
        //public Guid Id { get; set; }
        //public string FullName { get; set; }
        //public string Email { get; set; }
        //public string? Phone { get; set; }
        //public bool IsVerified { get; set; } = false!;
        //public string Password { get; set; }

        //public string? AvatarUrl { get; set; }
        //public int? Gender { get; set; }
        //public DateTime? DateOfBirth { get; set; }
        //public string? Address { get; set; }
        //public StatusEnum Status { get; set; }
        //public Guid RoleId { get; set; }
        //public Guid? OrganizerId { get; set; }
    }
}
