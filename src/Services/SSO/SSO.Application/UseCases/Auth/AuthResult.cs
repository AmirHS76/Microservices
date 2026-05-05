namespace SSO.Application.UseCases.Auth;

public sealed record AuthResult(bool Success, string? Token, IEnumerable<string> Errors);
