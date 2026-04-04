using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public abstract record IntegrationEvent
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;
    }
}
