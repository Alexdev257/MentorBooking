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
            throw new NotImplementedException();
        }
    }
}
