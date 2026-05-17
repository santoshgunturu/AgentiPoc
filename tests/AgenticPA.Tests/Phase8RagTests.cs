using AgenticPA.Services.Data;
using AgenticPA.Services.Rag;
using FluentAssertions;
using Xunit;

namespace AgenticPA.Tests;

public class Phase8RagTests
{
    [Fact]
    public async Task PolicyRag_FindsKneeMriPolicyBySemantic()
    {
        IPolicyRagService rag = new InMemoryPolicyRagService(new JsonDataStore());

        IReadOnlyList<PolicySearchResult> hits = await rag.SearchAsync("MRI knee imaging requirements", topK: 3);

        hits.Should().NotBeEmpty();
        hits[0].Policy.Cpt.Should().BeOneOf("73721", "73722", "73723");
        hits[0].Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PolicyRag_FindsLumbarSpinePolicyForBackPain()
    {
        IPolicyRagService rag = new InMemoryPolicyRagService(new JsonDataStore());

        IReadOnlyList<PolicySearchResult> hits = await rag.SearchAsync("lumbar spine disc radiculopathy", topK: 3);

        hits.Should().NotBeEmpty();
        hits[0].Policy.Cpt.Should().BeOneOf("72148", "72149", "63030");
    }
}
