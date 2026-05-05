using MediatR;
using SSO.Application.Contracts;
using SSO.Domain.Entities;

namespace SSO.Application.UseCases.Auth;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    ILoginAuditRepository auditRepository) : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await identityService.ValidateCredentialsAsync(command.Email, command.Password, cancellationToken);
        if (!result.Success || result.UserId is null || string.IsNullOrWhiteSpace(result.UserName))
        {
            return new AuthResult(false, null, ["Invalid credentials."]);
        }

        await auditRepository.AddAsync(new LoginAudit(result.UserId.Value, result.UserName, DateTime.UtcNow), cancellationToken);
        var token = jwtTokenGenerator.GenerateToken(result.UserId.Value, command.Email, result.Roles);
        return new AuthResult(true, token, []);
    }
}
