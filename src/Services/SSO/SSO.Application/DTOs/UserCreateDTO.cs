namespace SSO.Application.DTOs
{
    public record UserCreateDTO(Guid UserId, string Username, string Email, string Password);
}
