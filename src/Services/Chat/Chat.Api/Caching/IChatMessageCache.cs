using Chat.Api.Contracts;

namespace Chat.Api.Caching;

public interface IChatMessageCache
{
    Task SaveAsync(ChatMessageDto message, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessageDto>> GetInboxAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessageDto>> GetConversationAsync(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CachedConversationDto>> GetConversationsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task RemoveInboxConversationAsync(Guid recipientId, Guid senderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessageDto>> MarkInboxAsDeliveredAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChatMessageDto>> MarkInboxAsReadAsync(Guid recipientId, Guid senderId, CancellationToken cancellationToken = default);
    Task SaveConversationAsync(Guid currentUserId, Guid otherUserId, ChatMessageDto lastMessage, double score);
}
