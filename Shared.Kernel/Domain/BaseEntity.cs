using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Domain
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }

        [NotMapped]
        public List<DomainEvent> DomainEvents { get; private set; } = new();

        public void AddDomainEvent(DomainEvent domainEvent)
        {
            DomainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(DomainEvent domainEvent)
        {
            DomainEvents.Remove(domainEvent);
        }
    }

    public abstract class DomainEvent
    {
        public DateTime OccurredOn { get; private set; } = DateTime.UtcNow;
    }
}
