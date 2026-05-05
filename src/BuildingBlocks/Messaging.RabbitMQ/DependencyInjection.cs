using Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.RabbitMQ;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMqPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        return services;
    }

    public static IServiceCollection AddRabbitMqConsumer<TEvent, TConsumer>(
        this IServiceCollection services,
        IConfiguration configuration,
        string queueName)
        where TEvent : class, IIntegrationEvent
        where TConsumer : class, IEventConsumer<TEvent>
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddScoped<TConsumer>();
        services.AddHostedService(provider =>
            new RabbitMqConsumerBackgroundService<TEvent, TConsumer>(
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                queueName));

        return services;
    }
}
