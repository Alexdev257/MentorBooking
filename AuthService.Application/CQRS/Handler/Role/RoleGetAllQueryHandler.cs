using AuthService.Application.CQRS.Query.Role;
using AuthService.Application.DTOs.Response;
using AuthService.Domain.Enum;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.CQRS.Handler.Role
{
    public class RoleGetAllQueryHandler : IRequestHandler<RoleGetAllQuery, RoleGetAllResponse>
    {
        public Task<RoleGetAllResponse> Handle(RoleGetAllQuery request, CancellationToken cancellationToken)
        {
            var roles = Enum.GetValues(typeof(RoleNameEnum))
                .Cast<RoleNameEnum>()
                .Select(r => new RoleDTO
                {
                    Id = ((int)r).ToString(),
                    Name = r.ToString(),
                    Status = "Active",
                    CreatedAt = null
                })
                .ToList();

            return Task.FromResult(new RoleGetAllResponse
            {
                IsSuccess = true,
                Message = "Success",
                Data = roles
            });
        }
    }
}
