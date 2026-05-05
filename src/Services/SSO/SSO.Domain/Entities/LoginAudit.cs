namespace SSO.Domain.Entities;

public sealed class LoginAudit
{
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    private LoginAudit() { }

    public LoginAudit(Guid userId, string username, DateTime occurredAtUtc)
    {
        UserId = userId;
        Username = username;
        OccurredAtUtc = occurredAtUtc;
    }
}
