using Chat.Api.Contracts;

namespace Chat.Api.Caching;

public sealed record CachedConversationDto(Guid OtherUserId, ChatMessageDto LastMessage, DateTime LastMessageAtUtc);
