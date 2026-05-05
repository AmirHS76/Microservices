using System.Text;
using System.Text.Json;
using Messaging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Messaging.RabbitMQ;

public sealed class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher, IDisposable
{
    private readonly RabbitMqConnection _rabbitMq = RabbitMqConnection.Create(options.Value);

    private sealed record RabbitMqConnection(RabbitMqOptions Options, IConnection Connection, IModel Channel)
    {
        public static RabbitMqConnection Create(RabbitMqOptions options)
        {
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.ExchangeDeclare(options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
            return new RabbitMqConnection(options, connection, channel);
        }
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        var payload = JsonSerializer.Serialize(integrationEvent);
        var body = Encoding.UTF8.GetBytes(payload);
        var props = _rabbitMq.Channel.CreateBasicProperties();
        props.Persistent = true;

        _rabbitMq.Channel.BasicPublish(
            exchange: _rabbitMq.Options.ExchangeName,
            routingKey: integrationEvent.EventType,
            basicProperties: props,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _rabbitMq.Channel.Dispose();
        _rabbitMq.Connection.Dispose();
    }
}
