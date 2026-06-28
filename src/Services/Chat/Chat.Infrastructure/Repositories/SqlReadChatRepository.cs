using Chat.Application.Contracts;
using Chat.Domain.Entities;
using Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Repositories
{
    public sealed class SqlReadChatRepository(ChatReadDbContext dbContext) : IReadChatRepository
    {
        public async Task<IReadOnlyCollection<ChatUser>> GetUsersAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Users
                .AsNoTracking()
                .Where(x => x.UserId != currentUserId)
                .OrderBy(x => x.Username)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Conversation>> GetConversationsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Conversations
                .AsNoTracking()
                .Include(x => x.Messages.OrderByDescending(m => m.CreatedAtUtc).Take(1))
                .Where(x => x.FirstUserId == currentUserId || x.SecondUserId == currentUserId)
                .OrderByDescending(x => x.LastMessageAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<ChatMessage>> GetMessagesAsync(Guid currentUserId, Guid otherUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var normalized = NormalizePair(currentUserId, otherUserId);
            var skip = Math.Max(0, pageNumber - 1) * Math.Clamp(pageSize, 1, 100);

            return await dbContext.Messages
                .AsNoTracking()
                .Where(x => x.Conversation != null &&
                            x.Conversation.FirstUserId == normalized.FirstUserId &&
                            x.Conversation.SecondUserId == normalized.SecondUserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip(skip)
                .Take(Math.Clamp(pageSize, 1, 100))
                .ToListAsync(cancellationToken);
        }

        private static (Guid FirstUserId, Guid SecondUserId) NormalizePair(Guid firstUserId, Guid secondUserId)
            => firstUserId.CompareTo(secondUserId) <= 0
                ? (firstUserId, secondUserId)
                : (secondUserId, firstUserId);
    }
}
