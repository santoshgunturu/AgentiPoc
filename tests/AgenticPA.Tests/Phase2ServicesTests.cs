using AgenticPA.Services;
using AgenticPA.Services.Data;
using AgenticPA.Services.Models;
using FluentAssertions;
using Xunit;

namespace AgenticPA.Tests;

public class Phase2ServicesTests
{
    private static (JsonDataStore, IMemberService, IProcedureService, IRulesEngine) BuildFixture()
    {
        JsonDataStore store = new();
        return (store, new MemberService(store), new ProcedureService(store), new RulesEngine(store));
    }

    [Fact]
    public async Task MemberService_FindsThreeJanes()
    {
        (_, IMemberService members, _, _) = BuildFixture();

        IReadOnlyList<Member> result = await members.SearchAsync("Jane Smith");

        result.Should().HaveCount(3);
        result.Should().OnlyContain(m => m.FirstName == "Jane" && m.LastName == "Smith");
    }

    [Fact]
    public async Task ProcedureService_MriKneeRequiresAuth()
    {
        (_, _, IProcedureService procedures, _) = BuildFixture();

        bool required = await procedures.CheckAuthRequiredAsync("73721");

        required.Should().BeTrue();
    }

    [Fact]
    public async Task RulesEngine_AutoApprovesWhenAllGood()
    {
        (_, _, _, IRulesEngine rules) = BuildFixture();
        CanonicalPaRequest req = new(
            MemberId: "M1001",
            Cpt: "73721",
            RequestingNpi: "1111111111",
            FacilityNpi: "9990001",
            Icd10: "M17.12",
            ConservativeTreatmentWeeks: 8,
            Notes: "ice, NSAIDs");

        RulesEvaluation result = await rules.SubmitAsync(req);

        result.Outcome.Should().Be("auto-approve");
        result.Gaps.Should().BeEmpty();
    }

    [Fact]
    public async Task RulesEngine_PendsWhenPtInsufficient()
    {
        (_, _, _, IRulesEngine rules) = BuildFixture();
        CanonicalPaRequest req = new(
            MemberId: "M1001",
            Cpt: "73721",
            RequestingNpi: "1111111111",
            FacilityNpi: "9990001",
            Icd10: "M17.12",
            ConservativeTreatmentWeeks: 2,
            Notes: string.Empty);

        RulesEvaluation result = await rules.SubmitAsync(req);

        result.Outcome.Should().Be("pend");
        result.Gaps.Should().Contain("insufficient-conservative-treatment");
    }

    [Fact]
    public async Task RulesEngine_DeniesWhenDxMismatch()
    {
        (_, _, _, IRulesEngine rules) = BuildFixture();
        CanonicalPaRequest req = new(
            MemberId: "M1001",
            Cpt: "73721",
            RequestingNpi: "1111111111",
            FacilityNpi: "9990001",
            Icd10: "Z00.00",
            ConservativeTreatmentWeeks: 8,
            Notes: string.Empty);

        RulesEvaluation result = await rules.SubmitAsync(req);

        result.Outcome.Should().Be("deny");
        result.Gaps.Should().Contain("diagnosis-not-covered");
    }
}
