namespace AgenticPA.Web.Services;

/// <summary>
/// Storage contract for serialized PA workflow sessions. Implementations swap
/// between in-memory (dev/test), Cosmos DB, Redis, etc. without changing
/// the orchestration layer.
/// </summary>
public interface ISessionRepository
{
    Task<string?> GetAsync(string sessionId, CancellationToken ct = default);
    Task SaveAsync(string sessionId, string json, CancellationToken ct = default);
    Task DeleteAsync(string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListAsync(CancellationToken ct = default);
}
