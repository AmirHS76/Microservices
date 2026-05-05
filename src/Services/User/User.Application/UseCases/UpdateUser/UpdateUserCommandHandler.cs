using MediatR;
using User.Application.Contracts;

namespace User.Application.UseCases.UpdateUser;

public sealed class UpdateUserCommandHandler(IUserProfileRepository repository) : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetByIdAsync(command.UserId, cancellationToken);
        if (profile is null)
        {
            return false;
        }

        profile.Update(command.Username, command.Email);
        await repository.UpdateAsync(profile, cancellationToken);
        return true;
    }
}
