using AgenticPA.Services.Models;

namespace AgenticPA.Services.Rag;

public record PolicySearchResult(Policy Policy, double Score);

/// <summary>
/// Abstraction over policy retrieval. The in-process implementation uses keyword
/// match over <c>policies.json</c>. Swap with a Qdrant-backed implementation
/// (see <c>QdrantPolicyRagService</c>) in production for semantic search.
/// </summary>
public interface IPolicyRagService
{
    Task<IReadOnlyList<PolicySearchResult>> SearchAsync(string query, int topK = 3, CancellationToken ct = default);
}
