using MediatR;
using SSO.Application.Contracts;
using SSO.Application.DTOs;
using SSO.Domain.Entities;

namespace SSO.Application.UseCases.Auth;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    ILoginAuditRepository auditRepository) : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var loginRequest = new LoginRequestDto(command.Email, command.Username, command.Password);
        var result = await identityService.ValidateCredentialsAsync(loginRequest, cancellationToken);
        if (!result.Success || result.UserId is null || string.IsNullOrWhiteSpace(result.UserName) || string.IsNullOrWhiteSpace(result.Email))
        {
            return new AuthResult(false, null, ["Invalid credentials."]);
        }

        await auditRepository.AddAsync(new LoginAudit(result.UserId.Value, result.UserName, DateTime.UtcNow), cancellationToken);
        var token = jwtTokenGenerator.GenerateToken(result.UserId.Value, result.Email, result.Roles);
        return new AuthResult(true, token, []);
    }
}
