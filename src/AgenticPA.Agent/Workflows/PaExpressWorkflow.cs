using AgenticPA.Services;
using AgenticPA.Services.Models;
using Microsoft.Agents.AI.Workflows;

namespace AgenticPA.Agent.Workflows;

/// <summary>
/// State that travels along the express PA workflow edges. Immutable so each
/// executor returns a new copy with the next field populated.
/// </summary>
public record PaExpressState(
    string MemberId,
    string Cpt,
    string RequestingNpi,
    string FacilityNpi,
    string Icd10,
    int ConservativeTreatmentWeeks,
    string Notes,
    RulesEvaluation? PreflightResult = null,
    RulesEvaluation? SubmitResult = null,
    string? AuditCaseId = null);

/// <summary>
/// "Express mode" PA workflow built with Microsoft Agent Framework's
/// <see cref="AgentWorkflowBuilder"/>. Takes a fully-populated canonical PA
/// request and produces a final outcome end-to-end with no operator interaction.
///
/// Designed for API-driven submissions / batch processing — complements the
/// interactive Blazor flow which uses <see cref="StateMachine.PaWorkflowEngine"/>.
/// </summary>
public static class PaExpressWorkflow
{
    public static Workflow Build(IRulesEngine rulesEngine, IAuditService audit)
    {
        // Node 1: Pre-flight — dry-run the deterministic rules engine.
        var preflightFunc = (PaExpressState s) =>
        {
            CanonicalPaRequest req = new(s.MemberId, s.Cpt, s.RequestingNpi, s.FacilityNpi,
                s.Icd10, s.ConservativeTreatmentWeeks, s.Notes);
            RulesEvaluation eval = rulesEngine.PreviewAsync(req).GetAwaiter().GetResult();
            return s with { PreflightResult = eval };
        };

        // Node 2: Submit — only proceeds if pre-flight didn't deny.
        var submitFunc = (PaExpressState s) =>
        {
            CanonicalPaRequest req = new(s.MemberId, s.Cpt, s.RequestingNpi, s.FacilityNpi,
                s.Icd10, s.ConservativeTreatmentWeeks, s.Notes);
            RulesEvaluation eval = rulesEngine.SubmitAsync(req).GetAwaiter().GetResult();
            return s with { SubmitResult = eval };
        };

        // Node 3: Audit — record the case regardless of outcome.
        var auditFunc = (PaExpressState s) =>
        {
            CanonicalPaRequest req = new(s.MemberId, s.Cpt, s.RequestingNpi, s.FacilityNpi,
                s.Icd10, s.ConservativeTreatmentWeeks, s.Notes);
            string outcome = s.SubmitResult?.Outcome ?? "skipped";
            AuditRecord rec = audit.RecordAsync(req, outcome).GetAwaiter().GetResult();
            return s with { AuditCaseId = rec.CaseId };
        };

        var preflightNode = preflightFunc.BindAsExecutor("Preflight");
        var submitNode = submitFunc.BindAsExecutor("Submit");
        var auditNode = auditFunc.BindAsExecutor("Audit");

        return new WorkflowBuilder(preflightNode)
            // Skip submit if the rules engine denies up front.
            .AddEdge<PaExpressState>(preflightNode, submitNode,
                condition: s => s?.PreflightResult?.Outcome != "deny")
            // Always audit, even on a deny short-circuit.
            .AddEdge<PaExpressState>(preflightNode, auditNode,
                condition: s => s?.PreflightResult?.Outcome == "deny")
            .AddEdge(submitNode, auditNode)
            .Build();
    }
}
