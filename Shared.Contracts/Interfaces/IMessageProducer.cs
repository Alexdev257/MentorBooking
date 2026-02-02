using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Interfaces
{
    public interface IMessageProducer
    {
        Task PublishAsync<T>(T @message, CancellationToken cancellationToken = default)
            where T : IntegrationEvent;
    }
}
