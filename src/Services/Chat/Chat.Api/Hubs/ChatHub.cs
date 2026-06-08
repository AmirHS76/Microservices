using Chat.Api.Contracts;
using Chat.Api.Realtime;
using Chat.Application.Contracts;
using Chat.Domain.Entities;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Chat.Api.Hubs;

[Authorize]
public sealed class ChatHub(
    IEventPublisher eventPublisher,
    IChatRepository repository,
    IUserConnectionTracker connectionTracker) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId();
        if (userId is not null)
        {
            connectionTracker.Add(userId.Value, Context.ConnectionId);
            var delivered = await repository.MarkDeliveredAsync(userId.Value, Context.ConnectionAborted);
            foreach (var group in delivered.GroupBy(x => x.SenderId))
            {
                await Clients.User(group.Key.ToString()).SendAsync("MessagesDelivered", group.Select(x => x.Id).ToArray(), Context.ConnectionAborted);
            }
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId();
        if (userId is not null)
        {
            connectionTracker.Remove(userId.Value, Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendPrivateMessage(SendChatMessageRequest request)
    {
        var senderId = CurrentUserId();
        if (senderId is null || request.RecipientId == senderId.Value || string.IsNullOrWhiteSpace(request.Body))
        {
            return;
        }

        var messageId = request.ClientMessageId == Guid.Empty ? Guid.NewGuid() : request.ClientMessageId;
        var body = request.Body.Trim();
        if (body.Length > 4000)
        {
            body = body[..4000];
        }

        var createdAtUtc = DateTime.UtcNow;
        await Clients.Caller.SendAsync("MessageQueued", new ChatMessageDto(
            messageId,
            senderId.Value,
            request.RecipientId,
            body,
            "Pending",
            createdAtUtc,
            null,
            null));

        await eventPublisher.PublishAsync(new ChatMessageQueuedEvent(messageId, senderId.Value, request.RecipientId, body, createdAtUtc), Context.ConnectionAborted);
    }

    public async Task MarkConversationRead(Guid otherUserId)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return;
        }

        var readMessages = await repository.MarkReadAsync(userId.Value, otherUserId, Context.ConnectionAborted);
        if (readMessages.Count > 0)
        {
            await Clients.User(otherUserId.ToString()).SendAsync("MessagesRead", readMessages.Select(x => x.Id).ToArray(), Context.ConnectionAborted);
            await Clients.Caller.SendAsync("MessagesRead", readMessages.Select(x => x.Id).ToArray(), Context.ConnectionAborted);
        }
    }

    public static ChatMessageDto MapMessage(ChatMessage message)
        => new(
            message.Id,
            message.SenderId,
            message.RecipientId,
            message.Body,
            message.Status.ToString(),
            message.CreatedAtUtc,
            message.DeliveredAtUtc,
            message.ReadAtUtc);

    private Guid? CurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
