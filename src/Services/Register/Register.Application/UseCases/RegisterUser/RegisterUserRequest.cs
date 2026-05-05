using MediatR;

namespace Register.Application.UseCases.RegisterUser;

public sealed record RegisterUserRequest(string Username, string Email, string Password) : IRequest<Guid>;
