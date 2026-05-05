using SSO.Application.Contracts;
using Messaging.Abstractions;
using Messaging.Contracts;

namespace SSO.Infrastructure.Consumers;

public sealed class UserRegisteredConsumer(IIdentityService identityService) : IEventConsumer<UserRegisteredEvent>
{
    public Task ConsumeAsync(UserRegisteredEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        return identityService.CreateUserAsync(
            integrationEvent.UserId,
            integrationEvent.Email,
            integrationEvent.Password,
            cancellationToken);
    }
}
