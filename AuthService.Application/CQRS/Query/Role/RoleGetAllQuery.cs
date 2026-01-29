using AuthService.Application.DTOs.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.CQRS.Query.Role
{
    public class RoleGetAllQuery : IRequest<RoleGetAllResponse>
    {
    }
}
