using Chat.Domain.Entities;

namespace Chat.Application.Contracts;

public interface IWriteChatRepository
{
    Task UpsertUserAsync(ChatUser user, CancellationToken cancellationToken = default);
    Task<ChatMessage> SaveQueuedMessageAsync(Guid messageId, Guid senderId, Guid recipientId, string body, DateTime createdAtUtc, bool recipientOnline, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessage>> MarkDeliveredAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessage>> MarkReadAsync(Guid readerId, Guid senderId, CancellationToken cancellationToken = default);
}
