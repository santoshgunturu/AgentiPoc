namespace AgenticPA.Agent.StateMachine;

public class InvalidTransitionException : InvalidOperationException
{
    public InvalidTransitionException(PaState state, IWorkflowCommand command)
        : base($"Command {command.GetType().Name} is not valid in state {state}.")
    {
        State = state;
        Command = command;
    }

    public PaState State { get; }
    public IWorkflowCommand Command { get; }
}
