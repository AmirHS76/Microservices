using Chat.Api.Realtime;
using Chat.Application.Contracts;
using Messaging.Abstractions;
using Messaging.Contracts;

namespace Chat.Api.Consumers;

public sealed class ChatMessageQueuedConsumer(
    IWriteChatRepository repository,
    IUserConnectionTracker connectionTracker) : IEventConsumer<ChatMessageQueuedEvent>
{
    public async Task ConsumeAsync(ChatMessageQueuedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var recipientOnline = connectionTracker.IsOnline(integrationEvent.RecipientId);
        await repository.SaveQueuedMessageAsync(
            integrationEvent.MessageId,
            integrationEvent.SenderId,
            integrationEvent.RecipientId,
            integrationEvent.Body,
            integrationEvent.CreatedAtUtc,
            recipientOnline,
            cancellationToken);
    }
}
