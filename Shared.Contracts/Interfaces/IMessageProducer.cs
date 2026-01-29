using SharedContracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedContracts.Interfaces
{
    public interface IMessageProducer
    {
        Task PublishAsync<T>(T @message, CancellationToken cancellationToken = default)
            where T : IntegrationEvent;
    }
}
