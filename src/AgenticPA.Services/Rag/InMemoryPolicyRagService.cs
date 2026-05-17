using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services.Rag;

/// <summary>
/// Keyword-scored policy retrieval over <c>policies.json</c>. Splits the query
/// into tokens, counts how many appear in each policy's full text, and returns
/// the top-K by score. Good enough for the POC; replace with Qdrant + embeddings
/// for production semantic search.
/// </summary>
public class InMemoryPolicyRagService : IPolicyRagService
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "of", "for", "to", "and", "or", "is", "in", "on", "at", "with", "what", "are", "does"
    };

    private readonly JsonDataStore _store;

    public InMemoryPolicyRagService(JsonDataStore store) => _store = store;

    public Task<IReadOnlyList<PolicySearchResult>> SearchAsync(string query, int topK = 3, CancellationToken ct = default)
    {
        string q = (query ?? string.Empty).Trim();
        if (q.Length == 0)
            return Task.FromResult<IReadOnlyList<PolicySearchResult>>(Array.Empty<PolicySearchResult>());

        string[] tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !Stopwords.Contains(t))
            .ToArray();
        if (tokens.Length == 0)
            return Task.FromResult<IReadOnlyList<PolicySearchResult>>(Array.Empty<PolicySearchResult>());

        IReadOnlyList<PolicySearchResult> scored = _store.Policies
            .Select(p =>
            {
                string haystack = $"{p.Cpt} {p.Citation} {p.FullText}";
                int hits = tokens.Count(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
                return new PolicySearchResult(p, hits);
            })
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult(scored);
    }
}
