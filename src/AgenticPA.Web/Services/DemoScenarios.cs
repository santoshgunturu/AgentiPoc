using AgenticPA.Agent.StateMachine;

namespace AgenticPA.Web.Services;

public record DemoScenario(
    string Id,
    string Name,
    string Description,
    string Icon,
    string ExpectedOutcome,
    PaUrgency Urgency,
    IReadOnlyList<ScenarioStep> Steps);

public record ScenarioStep(PaState AtState, string UserMessage);

public static class DemoScenarios
{
    public static readonly IReadOnlyList<DemoScenario> All = new[]
    {
        new DemoScenario(
            Id: "happy",
            Name: "Happy path",
            Description: "Jane Smith · MRI knee · all criteria met",
            Icon: "✓",
            ExpectedOutcome: "auto-approve",
            Urgency: PaUrgency.Standard,
            Steps: new ScenarioStep[]
            {
                new(PaState.MemberPending,      "I need a PA for Jane Smith."),
                new(PaState.MemberPending,      "1978-04-12"),
                new(PaState.ProcedurePending,   "MRI left knee without contrast."),
                new(PaState.ReqProviderPending, "Dr. Ramirez"),
                new(PaState.FacilityPending,    "Capital Imaging"),
                new(PaState.ClinicalPending,    "M17.12, 8 weeks of PT, ice and NSAIDs"),
                new(PaState.Preflight,          "run preflight"),
                new(PaState.Submit,             "yes")
            }),
        new DemoScenario(
            Id: "pend",
            Name: "Pend → remediate",
            Description: "Insufficient PT — operator updates and resubmits",
            Icon: "⊘",
            ExpectedOutcome: "pend",
            Urgency: PaUrgency.Standard,
            Steps: new ScenarioStep[]
            {
                new(PaState.MemberPending,      "I need a PA for Jane Smith."),
                new(PaState.MemberPending,      "1985-11-03"),
                new(PaState.ProcedurePending,   "MRI left knee without contrast."),
                new(PaState.ReqProviderPending, "Dr. Ramirez"),
                new(PaState.FacilityPending,    "Capital Imaging"),
                new(PaState.ClinicalPending,    "M17.12, 2 weeks of PT, ice only"),
                new(PaState.Preflight,          "run preflight"),
                new(PaState.Submit,             "yes")
            }),
        new DemoScenario(
            Id: "deny",
            Name: "Deny (out-of-scope dx)",
            Description: "Diagnosis doesn't support the requested imaging",
            Icon: "✕",
            ExpectedOutcome: "deny",
            Urgency: PaUrgency.Expedited,
            Steps: new ScenarioStep[]
            {
                new(PaState.MemberPending,      "I need a PA for John Doe."),
                new(PaState.ProcedurePending,   "MRI left knee without contrast."),
                new(PaState.ReqProviderPending, "Dr. Ramirez"),
                new(PaState.FacilityPending,    "Capital Imaging"),
                new(PaState.ClinicalPending,    "Z00.00, 8 weeks of PT, routine checkup"),
                new(PaState.Preflight,          "run preflight"),
                new(PaState.Submit,             "yes")
            })
    };
}
