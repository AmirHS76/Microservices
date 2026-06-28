namespace SSO.Application.DTOs
{
    public sealed record LoginRequestDto(string? Email, string? Username, string Password);
}
