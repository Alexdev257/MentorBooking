using AuthService.Application.CQRS.Query.Role;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.CQRS.Handler.Role
{
    public class RoleGetAllQueryHandler : IRequestHandler<RoleGetAllQuery, RoleGetAllResponse>
    {
        private readonly IAuthUnitOfWork _unitOfWork;
        public RoleGetAllQueryHandler(IAuthUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<RoleGetAllResponse> Handle(RoleGetAllQuery request, CancellationToken cancellationToken)
        {
            var roles = _unitOfWork.Roles.GetAllAsync().Where(r => !r.IsDeleted);
            if(!roles.Any())
            {
                return new RoleGetAllResponse
                {
                    IsSuccess = true,
                    Message = "Role retrieve successfully",
                    Data = new List<RoleDTO>()
                };
            }
            var response = await roles.Select(r => new RoleDTO
            {
                Id = r.Id.ToString(),
                Name = r.Name.ToString(),
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
            }).ToListAsync();

            return new RoleGetAllResponse
            {
                IsSuccess = true,
                Message = "Role retrieve successfully",
                Data = response,
            };
        }
    }
}
