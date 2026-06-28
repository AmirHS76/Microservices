using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSO.Application.Contracts;
using SSO.Application.DTOs;
using SSO.Infrastructure.Identity;

namespace SSO.Infrastructure.Auth;

public sealed class IdentityService(UserManager<AppIdentityUser> userManager) : IIdentityService
{
    public async Task<CreateIdentityUserResultDto> CreateUserAsync(UserCreateDto userDto, CancellationToken cancellationToken = default)
    {
        var existing = await userManager.Users.FirstOrDefaultAsync(x => x.Email == userDto.Email || x.UserName == userDto.Username, cancellationToken);
        if (existing is not null)
        {
            return new CreateIdentityUserResultDto(false, ["User already exists."], []);
        }

        var user = new AppIdentityUser
        {
            Id = userDto.UserId,
            Email = userDto.Email,
            UserName = userDto.Username
        };

        var result = await userManager.CreateAsync(user, userDto.Password);
        if (!result.Succeeded)
        {
            return new CreateIdentityUserResultDto(false, result.Errors.Select(x => x.Description).ToArray(), []);
        }

        await userManager.AddToRoleAsync(user, "User");
        return new CreateIdentityUserResultDto(true, [], ["User"]);
    }

    public async Task<ValidateCredentialsResultDto> ValidateCredentialsAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Email == loginRequest.Email || x.UserName == loginRequest.Username, cancellationToken);
        if (user is null)
        {
            return new ValidateCredentialsResultDto(false, null, null, null, []);
        }

        var ok = await userManager.CheckPasswordAsync(user, loginRequest.Password);
        if (!ok)
        {
            return new ValidateCredentialsResultDto(false, null, null, null, []);
        }

        var roles = await userManager.GetRolesAsync(user);
        return new ValidateCredentialsResultDto(true, user.Id, user.UserName, user.Email, roles.ToArray());
    }

    public async Task<AssignRoleResultDto> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return new AssignRoleResultDto(false, ["User not found."]);
        }

        var result = await userManager.AddToRoleAsync(user, role);
        return result.Succeeded
            ? new AssignRoleResultDto(true, [])
            : new AssignRoleResultDto(false, result.Errors.Select(x => x.Description).ToArray());
    }
}
