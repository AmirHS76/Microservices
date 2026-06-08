namespace Chat.Domain.Entities;

public sealed class Conversation
{
    public Guid Id { get; set; }
    public Guid FirstUserId { get; set; }
    public Guid SecondUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastMessageAtUtc { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = [];
}
