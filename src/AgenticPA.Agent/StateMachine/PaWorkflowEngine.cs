using AgenticPA.Services.Models;

namespace AgenticPA.Agent.StateMachine;

public class PaWorkflowEngine
{
    private readonly IRulesEngineClient _rules;

    public PaWorkflowEngine(IRulesEngineClient rules) => _rules = rules;

    public async Task<PaWorkflowContext> HandleAsync(
        PaWorkflowContext ctx,
        IWorkflowCommand cmd,
        CancellationToken ct = default)
    {
        if (cmd is GoToStateCommand go)
        {
            return Rewind(ctx, go.Target);
        }

        return (ctx.State, cmd) switch
        {
            (PaState.MemberPending, SetMemberCommand c) => ctx with { State = PaState.ProcedurePending, MemberId = c.MemberId },
            (PaState.ProcedurePending, SetProcedureCommand c) => ctx with { State = PaState.ReqProviderPending, Cpt = c.Cpt },
            (PaState.ReqProviderPending, SetRequestingProviderCommand c) => ctx with { State = PaState.FacilityPending, RequestingNpi = c.Npi },
            (PaState.FacilityPending, SetFacilityCommand c) => ctx with { State = PaState.ClinicalPending, FacilityNpi = c.FacilityNpi },
            (PaState.ClinicalPending, SetClinicalCommand c) => ctx with { State = PaState.Preflight, Icd10 = c.Icd10, ConservativeTreatmentWeeks = c.WeeksPt, Notes = c.Notes },
            (PaState.Preflight, RunPreflightCommand) => await RunPreflightAsync(ctx, ct),
            (PaState.Submit, SubmitCommand) => await RunSubmitAsync(ctx, ct),
            _ => throw new InvalidTransitionException(ctx.State, cmd)
        };
    }

    private static PaWorkflowContext Rewind(PaWorkflowContext ctx, PaState target)
    {
        if (target > ctx.State)
        {
            throw new InvalidTransitionException(ctx.State, new GoToStateCommand(target));
        }
        return target switch
        {
            PaState.MemberPending      => ctx with { State = target, MemberId = null, Cpt = null, RequestingNpi = null, FacilityNpi = null, Icd10 = null, ConservativeTreatmentWeeks = null, Notes = null, PreflightResult = null, SubmitResult = null },
            PaState.ProcedurePending   => ctx with { State = target, Cpt = null, RequestingNpi = null, FacilityNpi = null, Icd10 = null, ConservativeTreatmentWeeks = null, Notes = null, PreflightResult = null, SubmitResult = null },
            PaState.ReqProviderPending => ctx with { State = target, RequestingNpi = null, FacilityNpi = null, Icd10 = null, ConservativeTreatmentWeeks = null, Notes = null, PreflightResult = null, SubmitResult = null },
            PaState.FacilityPending    => ctx with { State = target, FacilityNpi = null, Icd10 = null, ConservativeTreatmentWeeks = null, Notes = null, PreflightResult = null, SubmitResult = null },
            PaState.ClinicalPending    => ctx with { State = target, Icd10 = null, ConservativeTreatmentWeeks = null, Notes = null, PreflightResult = null, SubmitResult = null },
            PaState.Preflight          => ctx with { State = target, PreflightResult = null, SubmitResult = null },
            _                          => throw new InvalidTransitionException(ctx.State, new GoToStateCommand(target))
        };
    }

    private async Task<PaWorkflowContext> RunPreflightAsync(PaWorkflowContext ctx, CancellationToken ct)
    {
        CanonicalPaRequest req = ToCanonical(ctx);
        RulesEvaluation eval = await _rules.PreviewAsync(req, ct);
        return ctx with { State = PaState.Submit, PreflightResult = eval };
    }

    private async Task<PaWorkflowContext> RunSubmitAsync(PaWorkflowContext ctx, CancellationToken ct)
    {
        CanonicalPaRequest req = ToCanonical(ctx);
        RulesEvaluation eval = await _rules.SubmitAsync(req, ct);
        return ctx with { State = PaState.Done, SubmitResult = eval };
    }

    private static CanonicalPaRequest ToCanonical(PaWorkflowContext ctx) => new(
        MemberId: ctx.MemberId ?? throw new InvalidOperationException("MemberId missing"),
        Cpt: ctx.Cpt ?? throw new InvalidOperationException("Cpt missing"),
        RequestingNpi: ctx.RequestingNpi ?? throw new InvalidOperationException("RequestingNpi missing"),
        FacilityNpi: ctx.FacilityNpi ?? throw new InvalidOperationException("FacilityNpi missing"),
        Icd10: ctx.Icd10 ?? throw new InvalidOperationException("Icd10 missing"),
        ConservativeTreatmentWeeks: ctx.ConservativeTreatmentWeeks ?? throw new InvalidOperationException("Weeks missing"),
        Notes: ctx.Notes ?? string.Empty);
}
