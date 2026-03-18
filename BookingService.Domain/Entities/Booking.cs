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
        public string Currency { get; set; } = "VND";
        public DateTime ScheduleStart { get; set; }
        public DateTime ScheduleEnd { get; set; }
        /// <summary>Google Meet or other meeting link (set when booking is accepted and calendar event is created).</summary>
        public string? MeetingLink { get; set; }
        /// <summary>Google Calendar event id (for updates/cancellation).</summary>
        public string? GoogleEventId { get; set; }
        public virtual AvailabilitySlot? Slot { get; set; }

    }
}
