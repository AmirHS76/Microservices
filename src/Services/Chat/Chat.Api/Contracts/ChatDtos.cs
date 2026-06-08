namespace Chat.Api.Contracts;

public sealed record ChatUserDto(Guid UserId, string Username, string Email);

public sealed record ChatMessageDto(
    Guid Id,
    Guid SenderId,
    Guid RecipientId,
    string Body,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? ReadAtUtc);

public sealed record ConversationDto(
    Guid Id,
    Guid OtherUserId,
    string OtherUsername,
    string OtherEmail,
    ChatMessageDto? LastMessage,
    DateTime LastMessageAtUtc);

public sealed record SendChatMessageRequest(Guid RecipientId, string Body, Guid ClientMessageId);
