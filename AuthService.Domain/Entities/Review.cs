using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Entities
{
    public class Review : AuditableEntity
    {
        public Guid BookingId { get; set; }

        public Guid MentorId { get; set; }
        public Guid MenteeId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual User Mentor { get; set; }
        public virtual User Mentee { get; set; }
    }
}
