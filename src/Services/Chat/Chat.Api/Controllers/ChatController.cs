using ApiResponses;
using Chat.Api.Caching;
using Chat.Api.Contracts;
using Chat.Api.Hubs;
using Chat.Application.UseCases.GetChatUsers;
using Chat.Application.UseCases.GetConversations;
using Chat.Application.UseCases.GetMessages;
using Chat.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Chat.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ChatController(IMediator mediator, IChatMessageCache messageCache) : ControllerBase
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
            return Unauthorized(ApiResponse<object>.Fail(["Authenticated user id is missing."], "Unauthorized."));

        var users = await mediator.Send(new GetChatUsersQuery(currentUserId.Value), cancellationToken);
        var userLookup = users.ToDictionary(x => x.UserId);

        var cachedConversations = await messageCache.GetConversationsAsync(currentUserId.Value, cancellationToken);
        if (cachedConversations.Count > 0)
        {
            var cachedResponse = cachedConversations
                .Select(x =>
                {
                    userLookup.TryGetValue(x.OtherUserId, out var otherUser);
                    return new ConversationDto(
                        Guid.Empty,
                        x.OtherUserId,
                        otherUser?.Username ?? string.Empty,
                        otherUser?.Email ?? string.Empty,
                        x.LastMessage,
                        x.LastMessageAtUtc);
                })
                .OrderByDescending(x => x.LastMessageAtUtc)
                .ToArray();

            return Ok(ApiResponse<IReadOnlyCollection<ConversationDto>>.Ok(cachedResponse, "Conversations returned successfully."));
        }

        var conversations = await mediator.Send(new GetConversationsQuery(currentUserId.Value), cancellationToken);
        var response = conversations
            .Select(x =>
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
            })
            .OrderByDescending(x => x.LastMessageAtUtc)
            .ToArray();
        foreach (var item in response)
            if (item.LastMessage != null)
                await messageCache.SaveConversationAsync(currentUserId.Value, item.OtherUserId, item.LastMessage, new DateTimeOffset(item.LastMessageAtUtc).ToUnixTimeMilliseconds());

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
            return Unauthorized(ApiResponse<object>.Fail(["Authenticated user id is missing."], "Unauthorized."));

        var pageNumber = pagination.PageNumber <= 0 ? 1 : pagination.PageNumber;
        var pageSize = pagination.PageSize <= 0 ? 30 : pagination.PageSize;
        var clampedSize = Math.Clamp(pageSize, 1, 100);

        IEnumerable<ChatMessageDto> response;

        if (pageNumber == 1)
        {
            var cachedMessages = await messageCache.GetConversationAsync(currentUserId.Value, otherUserId, cancellationToken);

            if (cachedMessages.Count >= clampedSize)
            {
                response = cachedMessages
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Take(clampedSize);
            }
            else
            {
                var dbMessages = await mediator.Send(new GetMessagesQuery(currentUserId.Value, otherUserId, pageNumber, pageSize), cancellationToken);
                response = MergeMessages(dbMessages.Select(ChatHub.MapMessage), cachedMessages)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Take(clampedSize);
            }
        }
        else
        {
            var dbMessages = await mediator.Send(new GetMessagesQuery(currentUserId.Value, otherUserId, pageNumber, pageSize), cancellationToken);
            response = dbMessages.Select(ChatHub.MapMessage);
        }

        var responseArray = response.ToArray();
        return Ok(ApiResponse<IReadOnlyCollection<ChatMessageDto>>.Ok(responseArray, "Messages returned successfully."));
    }

    private static IReadOnlyCollection<ChatMessageDto> MergeMessages(
        IEnumerable<ChatMessageDto> databaseMessages,
        IEnumerable<ChatMessageDto> cachedMessages)
    {
        var messages = databaseMessages.ToDictionary(x => x.Id);
        foreach (var message in cachedMessages)
        {
            messages[message.Id] = message;
        }

        return messages.Values;
    }

    private static void MergeCachedConversations(
        List<ConversationDto> conversations,
        IEnumerable<CachedConversationDto> cachedConversations,
        IReadOnlyDictionary<Guid, ChatUser> userLookup)
    {
        var indexes = conversations
            .Select((conversation, index) => new { conversation.OtherUserId, Index = index })
            .ToDictionary(x => x.OtherUserId, x => x.Index);

        foreach (var cached in cachedConversations)
        {
            if (indexes.TryGetValue(cached.OtherUserId, out var index))
            {
                if (cached.LastMessageAtUtc > conversations[index].LastMessageAtUtc)
                {
                    var existing = conversations[index];
                    conversations[index] = existing with
                    {
                        LastMessage = cached.LastMessage,
                        LastMessageAtUtc = cached.LastMessageAtUtc
                    };
                }

                continue;
            }

            userLookup.TryGetValue(cached.OtherUserId, out var otherUser);
            indexes[cached.OtherUserId] = conversations.Count;
            conversations.Add(new ConversationDto(
                Guid.Empty,
                cached.OtherUserId,
                otherUser?.Username ?? string.Empty,
                otherUser?.Email ?? string.Empty,
                cached.LastMessage,
                cached.LastMessageAtUtc));
        }
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
