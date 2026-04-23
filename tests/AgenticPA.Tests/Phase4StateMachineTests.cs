using AgenticPA.Agent.StateMachine;
using AgenticPA.Services.Models;
using FluentAssertions;
using Xunit;

namespace AgenticPA.Tests;

public class Phase4StateMachineTests
{
    private sealed class StubRulesClient : IRulesEngineClient
    {
        private readonly RulesEvaluation _preview;
        private readonly RulesEvaluation _submit;
        public StubRulesClient(RulesEvaluation preview, RulesEvaluation submit) { _preview = preview; _submit = submit; }
        public Task<RulesEvaluation> PreviewAsync(CanonicalPaRequest request, CancellationToken ct) => Task.FromResult(_preview);
        public Task<RulesEvaluation> SubmitAsync(CanonicalPaRequest request, CancellationToken ct) => Task.FromResult(_submit);
    }

    private static PaWorkflowEngine NewEngine(RulesEvaluation? preview = null, RulesEvaluation? submit = null)
    {
        RulesEvaluation ok = new("auto-approve", Array.Empty<string>(), "all good");
        return new PaWorkflowEngine(new StubRulesClient(preview ?? ok, submit ?? ok));
    }

    [Fact]
    public async Task StateMachine_AdvancesMemberToProcedure()
    {
        PaWorkflowEngine engine = NewEngine();
        PaWorkflowContext ctx = PaWorkflowContext.Initial();

        PaWorkflowContext next = await engine.HandleAsync(ctx, new SetMemberCommand("M1001"));

        next.State.Should().Be(PaState.ProcedurePending);
        next.MemberId.Should().Be("M1001");
    }

    [Fact]
    public async Task StateMachine_RejectsOutOfOrderCommand()
    {
        PaWorkflowEngine engine = NewEngine();
        PaWorkflowContext ctx = PaWorkflowContext.Initial();

        Func<Task> act = () => engine.HandleAsync(ctx, new SetFacilityCommand("9990001"));

        await act.Should().ThrowAsync<InvalidTransitionException>();
    }

    [Fact]
    public async Task StateMachine_PreflightReturnsRulesOutcome()
    {
        RulesEvaluation canned = new("auto-approve", Array.Empty<string>(), "preview ok");
        PaWorkflowEngine engine = NewEngine(preview: canned);

        PaWorkflowContext ctx = PaWorkflowContext.Initial();
        ctx = await engine.HandleAsync(ctx, new SetMemberCommand("M1001"));
        ctx = await engine.HandleAsync(ctx, new SetProcedureCommand("73721"));
        ctx = await engine.HandleAsync(ctx, new SetRequestingProviderCommand("1111111111"));
        ctx = await engine.HandleAsync(ctx, new SetFacilityCommand("9990001"));
        ctx = await engine.HandleAsync(ctx, new SetClinicalCommand("M17.12", 8, "ice, NSAIDs"));
        ctx.State.Should().Be(PaState.Preflight);

        ctx = await engine.HandleAsync(ctx, new RunPreflightCommand());

        ctx.State.Should().Be(PaState.Submit);
        ctx.PreflightResult.Should().NotBeNull();
        ctx.PreflightResult!.Outcome.Should().Be("auto-approve");
    }
}
