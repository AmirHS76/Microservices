using Chat.Domain.Entities;

namespace Chat.Application.Contracts;

public interface IChatRepository
{
    Task UpsertUserAsync(ChatUser user, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatUser>> GetUsersAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Conversation>> GetConversationsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessage>> GetMessagesAsync(Guid currentUserId, Guid otherUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ChatMessage> SaveQueuedMessageAsync(Guid messageId, Guid senderId, Guid recipientId, string body, DateTime createdAtUtc, bool recipientOnline, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessage>> MarkDeliveredAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessage>> MarkReadAsync(Guid readerId, Guid senderId, CancellationToken cancellationToken = default);
}
