using AuthService.Domain.Enum;
using SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Entities
{
    public class Role : AuditableEntity
    {
        public RoleNameEnum Name { get; set; }
        public StatusEnum Status { get; set; } = StatusEnum.Active;
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
