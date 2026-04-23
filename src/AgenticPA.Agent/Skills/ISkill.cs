using AgenticPA.Agent.StateMachine;

namespace AgenticPA.Agent.Skills;

public interface ISkill
{
    PaState Handles { get; }
    Task<SkillResponse> HandleTurnAsync(
        PaWorkflowContext ctx,
        IReadOnlyList<ChatTurn> transcript,
        string userMessage,
        CancellationToken ct);
}

public record SkillResponse(string ReplyToUser, IWorkflowCommand? CommandToApply);

public record ChatTurn(string Role, string Content);
