namespace AgenticPA.Agent.StateMachine;

public interface IWorkflowCommand;

public sealed record SetMemberCommand(string MemberId) : IWorkflowCommand;
public sealed record SetProcedureCommand(string Cpt) : IWorkflowCommand;
public sealed record SetRequestingProviderCommand(string Npi) : IWorkflowCommand;
public sealed record SetFacilityCommand(string FacilityNpi) : IWorkflowCommand;
public sealed record SetClinicalCommand(string Icd10, int WeeksPt, string Notes) : IWorkflowCommand;
public sealed record RunPreflightCommand : IWorkflowCommand;
public sealed record SubmitCommand : IWorkflowCommand;
public sealed record GoToStateCommand(PaState Target) : IWorkflowCommand;
