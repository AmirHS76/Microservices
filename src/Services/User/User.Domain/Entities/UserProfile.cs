namespace User.Domain.Entities;

public sealed class UserProfile
{
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private UserProfile() { }

    public UserProfile(Guid userId, string username, string email)
    {
        UserId = userId;
        Username = username;
        Email = email;
    }

    public void Update(string username, string email)
    {
        Username = username;
        Email = email;
    }
}
