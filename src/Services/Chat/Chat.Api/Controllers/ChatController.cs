using ApiResponses;
using Chat.Api.Contracts;
using Chat.Api.Hubs;
using Chat.Application.UseCases.GetChatUsers;
using Chat.Application.UseCases.GetConversations;
using Chat.Application.UseCases.GetMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Chat.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ChatController(IMediator mediator) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail(["Authenticated user id is missing."], "Unauthorized."));
        }

        var users = await mediator.Send(new GetChatUsersQuery(currentUserId.Value), cancellationToken);
        var response = users.Select(x => new ChatUserDto(x.UserId, x.Username, x.Email)).ToArray();
        return Ok(ApiResponse<IReadOnlyCollection<ChatUserDto>>.Ok(response, "Chat users returned successfully."));
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail(["Authenticated user id is missing."], "Unauthorized."));
        }

        var conversations = await mediator.Send(new GetConversationsQuery(currentUserId.Value), cancellationToken);
        var users = await mediator.Send(new GetChatUsersQuery(currentUserId.Value), cancellationToken);
        var userLookup = users.ToDictionary(x => x.UserId);
        var response = conversations.Select(x =>
        {
            var otherUserId = x.FirstUserId == currentUserId.Value ? x.SecondUserId : x.FirstUserId;
            userLookup.TryGetValue(otherUserId, out var otherUser);
            var lastMessage = x.Messages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
            return new ConversationDto(
                x.Id,
                otherUserId,
                otherUser?.Username ?? string.Empty,
                otherUser?.Email ?? string.Empty,
                lastMessage is null ? null : ChatHub.MapMessage(lastMessage),
                x.LastMessageAtUtc);
        }).ToArray();

        return Ok(ApiResponse<IReadOnlyCollection<ConversationDto>>.Ok(response, "Conversations returned successfully."));
    }

    [HttpGet("messages/{otherUserId:guid}")]
    public async Task<IActionResult> GetMessages(
        Guid otherUserId,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail(["Authenticated user id is missing."], "Unauthorized."));
        }

        var pageNumber = pagination.PageNumber <= 0 ? 1 : pagination.PageNumber;
        var pageSize = pagination.PageSize <= 0 ? 30 : pagination.PageSize;
        var messages = await mediator.Send(new GetMessagesQuery(currentUserId.Value, otherUserId, pageNumber, pageSize), cancellationToken);
        var response = messages.Select(ChatHub.MapMessage).ToArray();
        return Ok(ApiResponse<IReadOnlyCollection<ChatMessageDto>>.Ok(response, "Messages returned successfully."));
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
