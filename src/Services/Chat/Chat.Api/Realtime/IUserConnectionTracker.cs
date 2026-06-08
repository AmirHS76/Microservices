namespace Chat.Api.Realtime;

public interface IUserConnectionTracker
{
    void Add(Guid userId, string connectionId);
    void Remove(Guid userId, string connectionId);
    bool IsOnline(Guid userId);
}
