using ApiResponses;
using SSO.Api.Contracts;
using SSO.Application.UseCases.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SSO.Api.Controllers;

[ApiController]
[Route("api/sso/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await mediator.Send(command, cancellationToken);
        var response = result.Success
            ? ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto(result.Token!), "Login completed successfully.")
            : ApiResponse<AuthResponseDto>.Fail(result.Errors.ToArray(), "Login failed.");
        return result.Success ? Ok(response) : Unauthorized(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole(
        [FromBody] AssignRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new AssignRoleCommand(request.UserId, request.Role);
        var result = await mediator.Send(command, cancellationToken);
        var response = result.Success
            ? ApiResponse<object>.Ok(null, "Role assigned successfully.")
            : ApiResponse<object>.Fail(result.Errors.ToArray(), "Role assignment failed.");
        return result.Success ? Ok(response) : BadRequest(response);
    }
}
