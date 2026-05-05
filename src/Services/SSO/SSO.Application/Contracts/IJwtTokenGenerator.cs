namespace SSO.Application.Contracts;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, IReadOnlyCollection<string> roles);
}
