using MediatR;
using SSO.Application.Contracts;

namespace SSO.Application.UseCases.Auth;

public sealed class AssignRoleCommandHandler(IIdentityService identityService) : IRequestHandler<AssignRoleCommand, OperationResult>
{
    public async Task<OperationResult> Handle(AssignRoleCommand command, CancellationToken cancellationToken)
    {
        var result = await identityService.AssignRoleAsync(command.UserId, command.Role, cancellationToken);
        return new OperationResult(result.Success, result.Errors);
    }
}
