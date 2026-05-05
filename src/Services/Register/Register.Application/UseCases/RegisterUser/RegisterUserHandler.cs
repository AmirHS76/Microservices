using MediatR;
using Messaging.Abstractions;
using Messaging.Contracts;
using Register.Application.Contracts;
using Register.Domain.Entities;

namespace Register.Application.UseCases.RegisterUser;

public sealed class RegisterUserHandler(
    IUserRegistrationRepository repository,
    IEventPublisher eventPublisher) : IRequestHandler<RegisterUserRequest, Guid>
{
    public async Task<Guid> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var user = new RegisteredUser(Guid.NewGuid(), request.Username, request.Email);
        await repository.AddAsync(user, cancellationToken);

        var evt = new UserRegisteredEvent(user.Id, user.Username, user.Email, request.Password);
        await eventPublisher.PublishAsync(evt, cancellationToken);

        return user.Id;
    }
}
