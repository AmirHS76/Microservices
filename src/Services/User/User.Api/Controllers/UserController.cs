using ApiResponses;
using Microsoft.AspNetCore.Mvc;
using User.Api.Contracts;
using User.Application.UseCases.GetUsers;
using User.Application.UseCases.UpdateUser;
using MediatR;

namespace User.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UserController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var users = await mediator.Send(new GetUsersQuery(), cancellationToken);
        var userDtos = users
            .Select(x => new UserProfileDto(x.UserId, x.Username, x.Email))
            .ToArray();
        var page = userDtos.Paginate(pagination);
        var response = ApiResponse<IReadOnlyCollection<UserProfileDto>>.Ok(page.Items, "Users returned successfully.", page.Pagination);

        return Ok(response);
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserProfileDto request,
        CancellationToken cancellationToken)
    {
        var updated = await mediator.Send(new UpdateUserCommand(userId, request.Username, request.Email), cancellationToken);
        if (!updated)
        {
            return NotFound(ApiResponse<object>.Fail(["User not found."], "User update failed."));
        }

        return Ok(ApiResponse<object>.Ok(null, "User updated successfully."));
    }
}
