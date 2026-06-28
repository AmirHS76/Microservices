using Chat.Domain.Entities;

namespace Chat.Application.Contracts
{
    public interface IReadChatRepository
    {
        Task<IReadOnlyCollection<ChatUser>> GetUsersAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Conversation>> GetConversationsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ChatMessage>> GetMessagesAsync(Guid currentUserId, Guid otherUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
