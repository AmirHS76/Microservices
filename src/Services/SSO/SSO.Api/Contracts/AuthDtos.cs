namespace SSO.Api.Contracts;

public sealed record LoginRequestDto(string Email, string Password);
public sealed record AssignRoleRequestDto(Guid UserId, string Role);

public sealed record AuthResponseDto(string Token);
