using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Domain.Entities
{
    public class Job : AuditableEntity
    {
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public string? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public int Attempts { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime RunAfter { get; set; }

    }
}
