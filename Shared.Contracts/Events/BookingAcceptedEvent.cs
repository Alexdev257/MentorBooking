using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public record BookingAcceptedEvent(
        Guid BookingId,
        Guid MentorId,
        Guid MenteeId,
        string JoinUrl,
        DateTime StartedAt,
        DateTime EndedAt
    ) : IntegrationEvent;
}
