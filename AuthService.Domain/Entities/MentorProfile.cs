using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Entities
{
    public class MentorProfile : AuditableEntity
    {
        public Guid MentorId { get; set; }
        public string Headline { get; set; }
        public string Bio { get; set; }
        public string Languages { get; set; }
        public decimal HourlyRate { get; set; }
        public string Currency { get; set; }
        public bool IsActive { get; set; }
        public virtual User User { get; set; }
    }
}
