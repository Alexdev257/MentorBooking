using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIService.Domain.Entities
{
    public class Transcript : AuditableEntity
    {
        public Guid MeetingId { get; set; } 
        public int Status { get; set; }  
        public string? Language { get; set; } 
        public string? Model { get; set; }
        public virtual ICollection<TranscriptSegment> Segments { get; set; } = new List<TranscriptSegment>();
    }
}
