using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIService.Domain.Entities
{
    public class MeetingSummary : AuditableEntity
    {
        public Guid MeetingId { get; set; } 

        public string Summary { get; set; }

        public string KeyPoints { get; set; } = "[]";
        public string Topics { get; set; } = "[]";
        public string Sentiment { get; set; } = "{}";

        public string? Model { get; set; } 
    }
}
