using AgenticPA.Services.Models;

namespace AgenticPA.Agent.StateMachine;

public enum PaUrgency { Standard, Expedited, Retro }

public record PaWorkflowContext(
    PaState State,
    string? MemberId,
    string? Cpt,
    string? RequestingNpi,
    string? FacilityNpi,
    string? Icd10,
    int? ConservativeTreatmentWeeks,
    string? Notes,
    RulesEvaluation? PreflightResult,
    RulesEvaluation? SubmitResult,
    PaUrgency Urgency = PaUrgency.Standard,
    DateTime? StartedAt = null)
{
    public static PaWorkflowContext Initial()
        => new(PaState.MemberPending, null, null, null, null, null, null, null, null, null, PaUrgency.Standard, DateTime.UtcNow);
}
