using Messaging.Abstractions;
using Messaging.Contracts;
using SSO.Application.Contracts;
using SSO.Application.DTOs;

namespace SSO.Infrastructure.Consumers;

public sealed class UserRegisteredConsumer(IIdentityService identityService) : IEventConsumer<UserRegisteredEvent>
{
    public Task ConsumeAsync(UserRegisteredEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        return identityService.CreateUserAsync(new UserCreateDTO
            (integrationEvent.UserId, integrationEvent.Username, integrationEvent.Email, integrationEvent.Password), cancellationToken);
    }
}
