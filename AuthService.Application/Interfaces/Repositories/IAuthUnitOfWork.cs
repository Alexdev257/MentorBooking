using AuthService.Domain.Entities;
using SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces.Repositories
{
    public interface IAuthUnitOfWork : IUnitOfWork
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<Role> Roles { get; }
    }
}
