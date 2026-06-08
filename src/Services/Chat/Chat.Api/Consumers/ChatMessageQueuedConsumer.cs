using Chat.Api.Hubs;
using Chat.Api.Realtime;
using Chat.Application.Contracts;
using Chat.Domain.Entities;
using Messaging.Abstractions;
using Messaging.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Api.Consumers;

public sealed class ChatMessageQueuedConsumer(
    IChatRepository repository,
    IHubContext<ChatHub> hubContext,
    IUserConnectionTracker connectionTracker) : IEventConsumer<ChatMessageQueuedEvent>
{
    public async Task ConsumeAsync(ChatMessageQueuedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var recipientOnline = connectionTracker.IsOnline(integrationEvent.RecipientId);
        var message = await repository.SaveQueuedMessageAsync(
            integrationEvent.MessageId,
            integrationEvent.SenderId,
            integrationEvent.RecipientId,
            integrationEvent.Body,
            integrationEvent.CreatedAtUtc,
            recipientOnline,
            cancellationToken);

        var payload = ChatHub.MapMessage(message);
        await hubContext.Clients.User(message.SenderId.ToString()).SendAsync("MessageSaved", payload, cancellationToken);
        await hubContext.Clients.User(message.RecipientId.ToString()).SendAsync("ReceiveMessage", payload, cancellationToken);

        if (message.Status == ChatMessageStatus.Delivered)
        {
            await hubContext.Clients.User(message.SenderId.ToString()).SendAsync("MessagesDelivered", new[] { message.Id }, cancellationToken);
        }
    }
}
