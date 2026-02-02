using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingService.Domain.Entities
{
    public class Meeting : AuditableEntity
    {
        public Guid BookingId { get; set; }

        public int Status { get; set; }     
        public string Provider { get; set; }    

        public string? JoinUrl { get; set; }    
        public string? HostUrl { get; set; }    

        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public virtual ICollection<MeetingRecording> Recordings { get; set; } = new List<MeetingRecording>();
    }
}
