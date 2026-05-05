namespace SSO.Api.Contracts;

public sealed record LoginAuditDto(Guid UserId, string Username, DateTime OccurredAtUtc);
