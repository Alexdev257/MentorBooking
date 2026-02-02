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
        public string Timezone { get; set; }
        public virtual MentorProfile? MentorProfile { get; set; }
        public virtual ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
        public virtual ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();

    }

}
