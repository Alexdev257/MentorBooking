using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingService.Domain.Entities
{
    public class MeetingRecording : AuditableEntity
    {
        public Guid MeetingId { get; set; }
        public virtual Meeting Meeting { get; set; }

        public int Status { get; set; } 
        public string StorageUrl { get; set; }
        public string? ContentType { get; set; } 
        public int? DurationSeconds { get; set; }
        public long? SizeBytes { get; set; } 
    }
}
