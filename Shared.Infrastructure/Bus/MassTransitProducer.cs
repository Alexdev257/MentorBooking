using MassTransit;
using SharedContracts.Events;
using SharedContracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedInfrastructure.Bus
{
    public class MassTransitProducer : IMessageProducer
    {
        private readonly IPublishEndpoint _publishEndpoint;
        public MassTransitProducer(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }
        public Task PublishAsync<T>(T @message, CancellationToken cancellationToken = default) where T : IntegrationEvent
        {
            return _publishEndpoint.Publish(@message, cancellationToken);
        }
    }
}
