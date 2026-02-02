using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIService.Domain.Entities
{
    public class ActionItem : AuditableEntity
    {
        public Guid MeetingId { get; set; } 

        public Guid? AssigneeUserId { get; set; } 

        public string Text { get; set; }
        public DateTime? DueAt { get; set; }
        public string Status { get; set; } 
    }
}
