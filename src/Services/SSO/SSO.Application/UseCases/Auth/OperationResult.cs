namespace SSO.Application.UseCases.Auth;

public sealed record OperationResult(bool Success, IEnumerable<string> Errors);
