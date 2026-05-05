using MediatR;

namespace User.Application.UseCases.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string Username, string Email) : IRequest<bool>;
