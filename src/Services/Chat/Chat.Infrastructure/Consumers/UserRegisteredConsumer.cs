using Chat.Application.Contracts;
using Chat.Domain.Entities;
using Messaging.Abstractions;
using Messaging.Contracts;

namespace Chat.Infrastructure.Consumers;

public sealed class UserRegisteredConsumer(IChatRepository repository) : IEventConsumer<UserRegisteredEvent>
{
    public Task ConsumeAsync(UserRegisteredEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var user = new ChatUser
        {
            UserId = integrationEvent.UserId,
            Username = integrationEvent.Username,
            Email = integrationEvent.Email
        };

        return repository.UpsertUserAsync(user, cancellationToken);
    }
}
