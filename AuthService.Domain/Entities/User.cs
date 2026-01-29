using AuthService.Domain.Enum;
using SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Entities
{
    public class User : AuditableEntity
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public bool IsVerified { get; set; } = false!;
        public string Password { get; set; }

        public string? AvatarUrl { get; set; }
        public int? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public StatusEnum Status { get; set; }
        public Guid RoleId { get; set; }
        public virtual Role Role { get; set; }
        public Guid? OrganizerId { get; set; }
        public virtual ICollection<UserLocation> Locations { get; set; } = new List<UserLocation>();
    }

}
