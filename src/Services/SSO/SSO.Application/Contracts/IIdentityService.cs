using SSO.Application.DTOs;

namespace SSO.Application.Contracts;

public interface IIdentityService
{
    Task<CreateIdentityUserResultDto> CreateUserAsync(UserCreateDto createdUser, CancellationToken cancellationToken = default);
    Task<ValidateCredentialsResultDto> ValidateCredentialsAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken = default);
    Task<AssignRoleResultDto> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}
