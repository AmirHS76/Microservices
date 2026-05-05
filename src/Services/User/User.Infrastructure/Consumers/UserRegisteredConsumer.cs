using Messaging.Abstractions;
using Messaging.Contracts;
using User.Application.Contracts;
using User.Domain.Entities;

namespace User.Infrastructure.Consumers;

public sealed class UserRegisteredConsumer(IUserProfileRepository repository) : IEventConsumer<UserRegisteredEvent>
{
    public Task ConsumeAsync(UserRegisteredEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var profile = new UserProfile(integrationEvent.UserId, integrationEvent.Username, integrationEvent.Email);
        return repository.UpsertAsync(profile, cancellationToken);
    }
}
