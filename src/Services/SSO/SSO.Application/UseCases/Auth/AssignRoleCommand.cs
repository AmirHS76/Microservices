using MediatR;

namespace SSO.Application.UseCases.Auth;

public sealed record AssignRoleCommand(Guid UserId, string Role) : IRequest<OperationResult>;
