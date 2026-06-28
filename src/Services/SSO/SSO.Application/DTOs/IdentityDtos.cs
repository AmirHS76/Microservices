namespace SSO.Application.DTOs;

public sealed record CreateIdentityUserResultDto(
    bool Success,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<string> Roles);

public sealed record ValidateCredentialsResultDto(
    bool Success,
    Guid? UserId,
    string? UserName,
    string? Email,
    IReadOnlyCollection<string> Roles);

public sealed record AssignRoleResultDto(
    bool Success,
    IReadOnlyCollection<string> Errors);
