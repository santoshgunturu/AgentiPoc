using AgenticPA.Agent.Workflows;
using AgenticPA.Services;
using AgenticPA.Services.Data;
using FluentAssertions;
using Microsoft.Agents.AI.Workflows;
using Xunit;

namespace AgenticPA.Tests;

public class Phase3ExpressWorkflowTests
{
    [Fact]
    public async Task ExpressWorkflow_AutoApprovesCleanRequest()
    {
        JsonDataStore store = new();
        IRulesEngine rules = new RulesEngine(store);
        IAuditService audit = new AuditService();

        Workflow workflow = PaExpressWorkflow.Build(rules, audit);

        PaExpressState seed = new(
            MemberId: "M1001", Cpt: "73721",
            RequestingNpi: "1111111111", FacilityNpi: "9990001",
            Icd10: "M17.12", ConservativeTreatmentWeeks: 8, Notes: "ice, NSAIDs");

        PaExpressState? final = null;
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, seed);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is ExecutorCompletedEvent ev && ev.Data is PaExpressState s)
            {
                final = s;
            }
        }

        final.Should().NotBeNull();
        final!.PreflightResult!.Outcome.Should().Be("auto-approve");
        final.SubmitResult!.Outcome.Should().Be("auto-approve");
        final.AuditCaseId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExpressWorkflow_SkipsSubmitOnDeny()
    {
        JsonDataStore store = new();
        IRulesEngine rules = new RulesEngine(store);
        IAuditService audit = new AuditService();

        Workflow workflow = PaExpressWorkflow.Build(rules, audit);

        // Z00.00 is not a covered dx for CPT 73721 → rules engine denies up front.
        PaExpressState seed = new(
            MemberId: "M1001", Cpt: "73721",
            RequestingNpi: "1111111111", FacilityNpi: "9990001",
            Icd10: "Z00.00", ConservativeTreatmentWeeks: 8, Notes: "routine");

        PaExpressState? final = null;
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, seed);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is ExecutorCompletedEvent ev && ev.Data is PaExpressState s)
            {
                final = s;
            }
        }

        final.Should().NotBeNull();
        final!.PreflightResult!.Outcome.Should().Be("deny");
        final.SubmitResult.Should().BeNull(); // submit was skipped
        final.AuditCaseId.Should().NotBeNullOrWhiteSpace(); // but audit still ran
    }
}
