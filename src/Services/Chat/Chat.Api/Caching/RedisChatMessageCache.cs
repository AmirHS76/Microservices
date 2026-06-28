using Chat.Api.Contracts;
using Chat.Domain.Entities;
using StackExchange.Redis;
using System.Text.Json;

namespace Chat.Api.Caching;

public sealed class RedisChatMessageCache(IConnectionMultiplexer redis) : IChatMessageCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan MessageTtl = TimeSpan.FromMinutes(5);
    private readonly IDatabase database = redis.GetDatabase();

    public async Task SaveAsync(ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messageKey = MessageKey(message.Id);
        var inboxKey = InboxKey(message.RecipientId);
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        var score = new DateTimeOffset(message.CreatedAtUtc).ToUnixTimeMilliseconds();

        await database.StringSetAsync(messageKey, payload, MessageTtl);
        await database.SortedSetAddAsync(inboxKey, message.Id.ToString(), score);
        await database.KeyExpireAsync(inboxKey, MessageTtl);
        await AddConversationMessageAsync(message.SenderId, message.RecipientId, message.Id, score);
        await AddConversationMessageAsync(message.RecipientId, message.SenderId, message.Id, score);
        await SaveConversationAsync(message.SenderId, message.RecipientId, message, score);
        await SaveConversationAsync(message.RecipientId, message.SenderId, message, score);
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetInboxAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messageIds = await database.SortedSetRangeByRankAsync(InboxKey(recipientId));
        if (messageIds.Length == 0)
        {
            return [];
        }

        var keys = messageIds.Select(x => (RedisKey)MessageKey(x.ToString())).ToArray();
        var values = await database.StringGetAsync(keys);
        var messages = new List<ChatMessageDto>(values.Length);

        for (var index = 0; index < values.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!values[index].HasValue)
            {
                await database.SortedSetRemoveAsync(InboxKey(recipientId), messageIds[index]);
                continue;
            }

            var message = JsonSerializer.Deserialize<ChatMessageDto>(values[index].ToString(), JsonOptions);
            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetConversationAsync(
        Guid currentUserId,
        Guid otherUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messageIds = await database.SortedSetRangeByRankAsync(ConversationKey(currentUserId, otherUserId));
        return await GetMessagesAsync(messageIds, currentUserId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CachedConversationDto>> GetConversationsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var otherUserIds = await database.SortedSetRangeByRankAsync(ConversationsKey(currentUserId), order: Order.Descending);
        if (otherUserIds.Length == 0)
        {
            return [];
        }

        var conversations = new List<CachedConversationDto>(otherUserIds.Length);
        foreach (var otherUserIdValue in otherUserIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Guid.TryParse(otherUserIdValue.ToString(), out var otherUserId))
            {
                await database.SortedSetRemoveAsync(ConversationsKey(currentUserId), otherUserIdValue);
                continue;
            }

            var value = await database.StringGetAsync(ConversationSummaryKey(currentUserId, otherUserId));
            if (!value.HasValue)
            {
                await database.SortedSetRemoveAsync(ConversationsKey(currentUserId), otherUserIdValue);
                continue;
            }

            var conversation = JsonSerializer.Deserialize<CachedConversationDto>(value.ToString(), JsonOptions);
            if (conversation is not null)
            {
                conversations.Add(conversation);
            }
        }

        return conversations;
    }

    public async Task RemoveInboxConversationAsync(Guid recipientId, Guid senderId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inboxKey = InboxKey(recipientId);
        var messageIds = await database.SortedSetRangeByRankAsync(inboxKey);
        foreach (var messageId in messageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = await database.StringGetAsync(MessageKey(messageId.ToString()));
            if (!value.HasValue)
            {
                await database.SortedSetRemoveAsync(inboxKey, messageId);
                continue;
            }

            var message = JsonSerializer.Deserialize<ChatMessageDto>(value.ToString(), JsonOptions);
            if (message?.SenderId == senderId)
            {
                await database.SortedSetRemoveAsync(inboxKey, messageId);
            }
        }
    }

    private static string InboxKey(Guid recipientId) => $"chat:inbox:{recipientId}";

    private static string ConversationsKey(Guid userId) => $"chat:conversations:{userId}";

    private static string ConversationKey(Guid currentUserId, Guid otherUserId) => $"chat:conversation:{currentUserId}:{otherUserId}";

    private static string ConversationSummaryKey(Guid currentUserId, Guid otherUserId) => $"chat:conversation-summary:{currentUserId}:{otherUserId}";

    private static string MessageKey(Guid messageId) => $"chat:message:{messageId}";

    private static string MessageKey(string messageId) => $"chat:message:{messageId}";

    private async Task AddConversationMessageAsync(Guid currentUserId, Guid otherUserId, Guid messageId, double score)
    {
        var conversationKey = ConversationKey(currentUserId, otherUserId);

        await database.SortedSetAddAsync(conversationKey, messageId.ToString(), score);
        await database.KeyExpireAsync(conversationKey, MessageTtl);
    }

    public async Task SaveConversationAsync(Guid currentUserId, Guid otherUserId, ChatMessageDto lastMessage, double score)
    {
        var conversationsKey = ConversationsKey(currentUserId);
        var summaryKey = ConversationSummaryKey(currentUserId, otherUserId);
        var conversation = new CachedConversationDto(otherUserId, lastMessage, lastMessage.CreatedAtUtc);
        var payload = JsonSerializer.Serialize(conversation, JsonOptions);

        await database.StringSetAsync(summaryKey, payload, MessageTtl);
        await database.SortedSetAddAsync(conversationsKey, otherUserId.ToString(), score);
        await database.KeyExpireAsync(conversationsKey, MessageTtl);
    }

    private async Task<IReadOnlyCollection<ChatMessageDto>> GetMessagesAsync(
        RedisValue[] messageIds,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (messageIds.Length == 0)
        {
            return [];
        }

        var keys = messageIds.Select(x => (RedisKey)MessageKey(x.ToString())).ToArray();
        var values = await database.StringGetAsync(keys);
        var messages = new List<ChatMessageDto>(values.Length);

        for (var index = 0; index < values.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!values[index].HasValue)
            {
                await RemoveStaleConversationMessageAsync(currentUserId, messageIds[index]);
                continue;
            }

            var message = JsonSerializer.Deserialize<ChatMessageDto>(values[index].ToString(), JsonOptions);
            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    private async Task RemoveStaleConversationMessageAsync(Guid currentUserId, RedisValue messageId)
    {
        var otherUserIds = await database.SortedSetRangeByRankAsync(ConversationsKey(currentUserId));
        foreach (var otherUserId in otherUserIds)
        {
            if (Guid.TryParse(otherUserId.ToString(), out var parsedOtherUserId))
            {
                await database.SortedSetRemoveAsync(ConversationKey(currentUserId, parsedOtherUserId), messageId);
            }
            else
            {
                await database.SortedSetRemoveAsync(ConversationsKey(currentUserId), otherUserId);
            }
        }
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> MarkInboxAsDeliveredAsync(
    Guid recipientId,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inboxMessages = await GetInboxAsync(recipientId, cancellationToken);
        var updated = new List<ChatMessageDto>(inboxMessages.Count);

        foreach (var message in inboxMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (message.Status == ChatMessageStatus.Sent.ToString())
            {
                var deliveredMessage = message with
                {
                    Status = ChatMessageStatus.Delivered.ToString(),
                    DeliveredAtUtc = DateTime.UtcNow
                };
                var payload = JsonSerializer.Serialize(deliveredMessage, JsonOptions);
                await database.StringSetAsync(MessageKey(message.Id), payload, MessageTtl);
                updated.Add(deliveredMessage);
            }
            else
            {
                updated.Add(message);
            }
        }

        return updated;
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> MarkInboxAsReadAsync(
        Guid recipientId,
        Guid senderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inboxKey = InboxKey(recipientId);
        var messageIds = await database.SortedSetRangeByRankAsync(inboxKey);
        var read = new List<ChatMessageDto>();

        foreach (var messageId in messageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = await database.StringGetAsync(MessageKey(messageId.ToString()));
            if (!value.HasValue)
            {
                await database.SortedSetRemoveAsync(inboxKey, messageId);
                continue;
            }

            var message = JsonSerializer.Deserialize<ChatMessageDto>(value.ToString(), JsonOptions);
            if (message?.SenderId != senderId)
            {
                continue;
            }

            var readMessage = message with
            {
                Status = ChatMessageStatus.Read.ToString(),
                ReadAtUtc = DateTime.UtcNow
            };
            var payload = JsonSerializer.Serialize(readMessage, JsonOptions);
            await database.StringSetAsync(MessageKey(message.Id), payload, MessageTtl);
            await database.SortedSetRemoveAsync(inboxKey, messageId);
            read.Add(readMessage);
        }

        return read;
    }
}
