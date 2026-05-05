using ApiResponses;
using Microsoft.AspNetCore.Mvc;
using Register.Api.Contracts;
using Register.Application.UseCases.RegisterUser;
using MediatR;

namespace Register.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RegisterController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await mediator.Send(request, cancellationToken);
        var response = ApiResponse<RegisterResponseDto>.Ok(
            new RegisterResponseDto(userId, "User registered and event published."),
            "User registered and event published.");

        return Ok(response);
    }
}
