namespace SSO.Application.DTOs
{
    public record UserCreateDto(Guid UserId, string Username, string Email, string Password);
}
