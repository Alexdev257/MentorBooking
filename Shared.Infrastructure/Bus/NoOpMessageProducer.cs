using Shared.Contracts.Events;
using Shared.Contracts.Interfaces;

namespace Shared.Infrastructure.Bus;

/// <summary>
/// No-op implementation when RabbitMQ is not configured. Messages are not published.
/// </summary>
public class NoOpMessageProducer : IMessageProducer
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IntegrationEvent
    {
        return Task.CompletedTask;
    }
}
