using MediatR;

namespace SSO.Application.UseCases.Auth;

public sealed record LoginCommand(string? Email, string? Username, string Password) : IRequest<AuthResult>;
