using System.Collections.Concurrent;

namespace Chat.Api.Realtime;

public sealed class UserConnectionTracker : IUserConnectionTracker
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();
    private readonly object _sync = new();

    public void Add(Guid userId, string connectionId)
    {
        lock (_sync)
        {
            var connections = _connections.GetOrAdd(userId, _ => []);
            connections.Add(connectionId);
        }
    }

    public void Remove(Guid userId, string connectionId)
    {
        lock (_sync)
        {
            if (!_connections.TryGetValue(userId, out var connections))
            {
                return;
            }

            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _connections.TryRemove(userId, out _);
            }
        }
    }

    public bool IsOnline(Guid userId)
    {
        lock (_sync)
        {
            return _connections.TryGetValue(userId, out var connections) && connections.Count > 0;
        }
    }
}
