namespace Messaging.Abstractions;

public interface IEventConsumer<in TEvent>
    where TEvent : class, IIntegrationEvent
{
    Task ConsumeAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
