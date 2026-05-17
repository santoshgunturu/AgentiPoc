using System.Collections.Concurrent;

namespace AgenticPA.Web.Services;

/// <summary>
/// In-memory implementation. Survives within the process; swap for a Cosmos
/// or Redis implementation in production via DI registration.
/// </summary>
public class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<string, string> _store = new(StringComparer.OrdinalIgnoreCase);

    public Task<string?> GetAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(sessionId, out string? json) ? json : null);

    public Task SaveAsync(string sessionId, string json, CancellationToken ct = default)
    {
        _store[sessionId] = json;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        _store.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(_store.Keys.ToList());
}
