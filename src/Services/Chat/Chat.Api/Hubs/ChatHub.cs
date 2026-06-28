using Chat.Api.Caching;
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
    IWriteChatRepository repository,
    IChatMessageCache messageCache,
    IUserConnectionTracker connectionTracker) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId();
        if (userId is not null)
        {
            connectionTracker.Add(userId.Value, Context.ConnectionId);

            // Mark delivered in DB (for already-persisted messages)
            var dbDelivered = await repository.MarkDeliveredAsync(userId.Value, Context.ConnectionAborted);
            foreach (var group in dbDelivered.GroupBy(x => x.SenderId))
            {
                await Clients.User(group.Key.ToString()).SendAsync("MessagesDelivered", group.Select(x => x.Id).ToArray(), Context.ConnectionAborted);
            }

            // Mark delivered in cache (for in-flight messages not yet in DB)
            var cachedMessages = await messageCache.MarkInboxAsDeliveredAsync(userId.Value, Context.ConnectionAborted);
            var dbDeliveredIds = dbDelivered.Select(x => x.Id).ToHashSet();

            foreach (var group in cachedMessages.GroupBy(x => x.SenderId))
            {
                // Only notify for IDs not already covered by the DB path above
                var cacheOnlyIds = group.Select(x => x.Id).Where(id => !dbDeliveredIds.Contains(id)).ToArray();
                if (cacheOnlyIds.Length > 0)
                {
                    await Clients.User(group.Key.ToString()).SendAsync("MessagesDelivered", cacheOnlyIds, Context.ConnectionAborted);
                }
            }

            foreach (var message in cachedMessages)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", message, Context.ConnectionAborted);
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
        var isOnline = connectionTracker.IsOnline(request.RecipientId);

        var createdAtUtc = DateTime.UtcNow;
        var deliveredAtUtc = DateTime.UtcNow;
        var message = new ChatMessageDto(
            messageId,
            senderId.Value,
            request.RecipientId,
            body,
            isOnline ? ChatMessageStatus.Delivered.ToString() : ChatMessageStatus.Sent.ToString(),
            createdAtUtc,
            isOnline ? deliveredAtUtc : null,
            null);

        await messageCache.SaveAsync(message, Context.ConnectionAborted);
        await Clients.Caller.SendAsync("MessageSaved", message, Context.ConnectionAborted);
        if (isOnline)
            await Clients.Caller.SendAsync("MessagesDelivered", new[] { messageId }, Context.ConnectionAborted);
        await Clients.User(request.RecipientId.ToString()).SendAsync("ReceiveMessage", message, Context.ConnectionAborted);

        await eventPublisher.PublishAsync(new ChatMessageQueuedEvent(messageId, senderId.Value, request.RecipientId, body, createdAtUtc), Context.ConnectionAborted);
    }

    public async Task MarkConversationRead(Guid otherUserId)
    {
        var userId = CurrentUserId();
        if (userId is null)
            return;

        // Mark read in DB (persisted messages)
        var dbReadMessages = await repository.MarkReadAsync(userId.Value, otherUserId, Context.ConnectionAborted);
        var dbReadIds = dbReadMessages.Select(x => x.Id).ToHashSet();

        // Mark read in cache (in-flight messages not yet in DB), also cleans inbox
        var cachedReadMessages = await messageCache.MarkInboxAsReadAsync(userId.Value, otherUserId, Context.ConnectionAborted);
        var cacheOnlyIds = cachedReadMessages.Select(x => x.Id).Where(id => !dbReadIds.Contains(id)).ToArray();

        var allReadIds = dbReadIds.Concat(cacheOnlyIds).ToArray();
        if (allReadIds.Length > 0)
        {
            await Clients.User(otherUserId.ToString()).SendAsync("MessagesRead", allReadIds, Context.ConnectionAborted);
            await Clients.Caller.SendAsync("MessagesRead", allReadIds, Context.ConnectionAborted);
        }

        // No longer needed — MarkInboxAsReadAsync already removes from inbox
        // await messageCache.RemoveInboxConversationAsync(userId.Value, otherUserId, ...);
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
