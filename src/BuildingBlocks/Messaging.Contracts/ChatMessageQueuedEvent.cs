using Messaging.Abstractions;

namespace Messaging.Contracts;

public sealed record ChatMessageQueuedEvent(
    Guid MessageId,
    Guid SenderId,
    Guid RecipientId,
    string Body,
    DateTime CreatedAtUtc) : IIntegrationEvent;
