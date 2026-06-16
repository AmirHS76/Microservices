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
        if (await CheckExisting(request.Username, request.Email, cancellationToken))
            throw new InvalidOperationException("Username or email already exists.");

        var user = new RegisteredUser(Guid.NewGuid(), request.Username, request.Email);
        await repository.AddAsync(user, cancellationToken);

        var evt = new UserRegisteredEvent(user.Id, user.Username, user.Email, request.Password);
        await eventPublisher.PublishAsync(evt, cancellationToken);

        return user.Id;
    }

    private async Task<bool> CheckExisting(string username, string email, CancellationToken cancellationToken)
    {
        var existingByUsername = await repository.GetByUsernameAsync(username, cancellationToken);
        if (existingByUsername is not null)
        {
            return true;
        }
        var existingByEmail = await repository.GetByEmailAsync(email, cancellationToken);
        if (existingByEmail is not null)
        {
            return true;
        }
        return false;
    }
}
