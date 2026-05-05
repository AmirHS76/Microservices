namespace User.Api.Contracts;

public sealed record UserProfileDto(Guid UserId, string Username, string Email);
public sealed record UpdateUserProfileDto(string Username, string Email);
