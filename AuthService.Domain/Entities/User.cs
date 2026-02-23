using AuthService.Domain.Enum;
using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Entities
{
    public class User : AuditableEntity
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Fullname { get; set; }
        public int Role { get; set; }
        public string AvatarUrl { get; set; }
        public virtual Teacher? Teacher { get; set; }

    }

}
