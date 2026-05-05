using System.Text;
using System.Text.Json;
using Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messaging.RabbitMQ;

public sealed class RabbitMqConsumerBackgroundService<TEvent, TConsumer>(
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory,
    string queueName) : BackgroundService
    where TEvent : class, IIntegrationEvent
    where TConsumer : class, IEventConsumer<TEvent>
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, _options.ExchangeName, typeof(TEvent).Name);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var model = JsonSerializer.Deserialize<TEvent>(json);
            if (model is null || _channel is null)
            {
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<TConsumer>();
            await handler.ConsumeAsync(model, stoppingToken);
            _channel.BasicAck(args.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
