namespace AgenticPA.Agent.StateMachine;

public enum PaState
{
    MemberPending,
    ProcedurePending,
    ReqProviderPending,
    FacilityPending,
    ClinicalPending,
    Preflight,
    Submit,
    Done
}
