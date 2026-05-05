namespace Messaging.Abstractions;

public interface IIntegrationEvent
{
    string EventType => GetType().Name;
}
