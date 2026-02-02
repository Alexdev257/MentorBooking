using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Domain.Entities
{
    public class Booking : AuditableEntity
    {
        public Guid MentorId { get; set; }
        public Guid MenteeId { get; set; }
        public Guid? SlotId { get; set; }
        public int Status { get; set; }
        public string? Topic { get; set; }
        public string? Notes { get; set; }
        public decimal PriceAmount { get; set; }
        public string Currency { get; set; }
        public DateTime ScheduleStart { get; set; }
        public DateTime ScheduleEnd { get; set; }
        public virtual AvailabilitySlot? Slot { get; set; }

    }
}
