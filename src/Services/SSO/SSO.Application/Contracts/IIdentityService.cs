using SSO.Application.DTOs;

namespace SSO.Application.Contracts;

public interface IIdentityService
{
    Task<CreateIdentityUserResultDto> CreateUserAsync(UserCreateDTO createdUser, CancellationToken cancellationToken = default);
    Task<ValidateCredentialsResultDto> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AssignRoleResultDto> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}
