using Chat.Application.Contracts;
using Chat.Domain.Entities;
using Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Repositories;

public sealed class SqlChatRepository(ChatDbContext dbContext) : IChatRepository
{
    public async Task UpsertUserAsync(ChatUser user, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.UserId, cancellationToken);
        if (existing is null)
        {
            await dbContext.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

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

    public async Task<ChatMessage> SaveQueuedMessageAsync(Guid messageId, Guid senderId, Guid recipientId, string body, DateTime createdAtUtc, bool recipientOnline, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Messages.FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var conversation = await GetOrCreateConversationAsync(senderId, recipientId, createdAtUtc, cancellationToken);
        conversation.LastMessageAtUtc = createdAtUtc;

        var message = new ChatMessage
        {
            Id = messageId,
            ConversationId = conversation.Id,
            SenderId = senderId,
            RecipientId = recipientId,
            Body = body,
            CreatedAtUtc = createdAtUtc,
            Status = recipientOnline ? ChatMessageStatus.Delivered : ChatMessageStatus.Sent,
            DeliveredAtUtc = recipientOnline ? DateTime.UtcNow : null
        };

        await dbContext.Messages.AddAsync(message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<IReadOnlyCollection<ChatMessage>> MarkDeliveredAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.Messages
            .Where(x => x.RecipientId == recipientId && x.Status == ChatMessageStatus.Sent)
            .ToListAsync(cancellationToken);

        var deliveredAtUtc = DateTime.UtcNow;
        foreach (var message in messages)
        {
            message.Status = ChatMessageStatus.Delivered;
            message.DeliveredAtUtc = deliveredAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages;
    }

    public async Task<IReadOnlyCollection<ChatMessage>> MarkReadAsync(Guid readerId, Guid senderId, CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.Messages
            .Where(x => x.RecipientId == readerId &&
                        x.SenderId == senderId &&
                        x.Status != ChatMessageStatus.Read)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var message in messages)
        {
            message.Status = ChatMessageStatus.Read;
            message.DeliveredAtUtc ??= now;
            message.ReadAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages;
    }

    private async Task<Conversation> GetOrCreateConversationAsync(Guid firstUserId, Guid secondUserId, DateTime createdAtUtc, CancellationToken cancellationToken)
    {
        var normalized = NormalizePair(firstUserId, secondUserId);
        var conversation = await dbContext.Conversations.FirstOrDefaultAsync(
            x => x.FirstUserId == normalized.FirstUserId && x.SecondUserId == normalized.SecondUserId,
            cancellationToken);

        if (conversation is not null)
        {
            return conversation;
        }

        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            FirstUserId = normalized.FirstUserId,
            SecondUserId = normalized.SecondUserId,
            CreatedAtUtc = createdAtUtc,
            LastMessageAtUtc = createdAtUtc
        };

        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        return conversation;
    }

    private static (Guid FirstUserId, Guid SecondUserId) NormalizePair(Guid firstUserId, Guid secondUserId)
        => firstUserId.CompareTo(secondUserId) <= 0
            ? (firstUserId, secondUserId)
            : (secondUserId, firstUserId);
}
