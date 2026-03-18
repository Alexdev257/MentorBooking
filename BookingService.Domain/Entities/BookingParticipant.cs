using Shared.Kernel.Domain;
using System;

namespace BookingService.Domain.Entities
{
    public class BookingParticipant : AuditableEntity
    {
        public Guid BookingId { get; set; }
        public Guid? MenteeId { get; set; } // Nullable for external guests
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ZoomJoinUrl { get; set; }
        public string? ZoomRegistrantId { get; set; }

        public virtual Booking? Booking { get; set; }
    }
}
