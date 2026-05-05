namespace Register.Domain.Entities;

public sealed class RegisteredUser
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private RegisteredUser() { }

    public RegisteredUser(Guid id, string username, string email)
    {
        Id = id;
        Username = username;
        Email = email;
    }
}
