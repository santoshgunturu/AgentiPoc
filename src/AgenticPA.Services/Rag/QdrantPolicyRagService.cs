using AgenticPA.Services.Models;

namespace AgenticPA.Services.Rag;

/// <summary>
/// Scaffold for a Qdrant-backed semantic policy retrieval service.
///
/// To activate (production):
/// 1. Add a Qdrant resource to the Aspire AppHost:
///    <c>builder.AddContainer("qdrant", "qdrant/qdrant").WithEndpoint(port: 6334, scheme: "grpc");</c>
/// 2. Add packages: <c>Microsoft.SemanticKernel.Connectors.Qdrant</c>, <c>Microsoft.Extensions.AI.OpenAI</c>.
/// 3. Embed each Policy with <c>text-embedding-3-small</c> at startup and upsert into a "policies" collection.
/// 4. Implement <see cref="SearchAsync"/> by embedding the query and calling
///    <c>collection.SearchAsync(queryVector, topK)</c>.
/// 5. Register this class instead of <see cref="InMemoryPolicyRagService"/> in DI.
///
/// Until then this stub throws; the in-memory implementation is the default.
/// </summary>
public class QdrantPolicyRagService : IPolicyRagService
{
    public Task<IReadOnlyList<PolicySearchResult>> SearchAsync(string query, int topK = 3, CancellationToken ct = default)
        => throw new NotImplementedException(
            "Qdrant policy RAG is scaffolded but not wired. See class doc comment for activation steps.");
}
