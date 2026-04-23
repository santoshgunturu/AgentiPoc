using AgenticPA.Agent.Skills;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent;

public class Orchestrator
{
    private readonly PaWorkflowEngine _engine;
    private readonly IReadOnlyDictionary<PaState, ISkill> _skills;
    private readonly ILogger<Orchestrator> _logger;

    public Orchestrator(PaWorkflowEngine engine, IEnumerable<ISkill> skills, ILogger<Orchestrator> logger)
    {
        _engine = engine;
        _skills = skills.ToDictionary(s => s.Handles);
        _logger = logger;
    }

    public async Task<OrchestratorTurn> RunTurnAsync(
        PaWorkflowContext ctx,
        IReadOnlyList<ChatTurn> transcript,
        string userMessage,
        CancellationToken ct = default)
    {
        if (!_skills.TryGetValue(ctx.State, out ISkill? skill))
        {
            throw new InvalidOperationException($"No skill registered for state {ctx.State}");
        }

        SkillResponse resp = await skill.HandleTurnAsync(ctx, transcript, userMessage, ct);
        PaWorkflowContext next = ctx;

        string? cmdLog = null;
        if (resp.CommandToApply is not null)
        {
            cmdLog = resp.CommandToApply.GetType().Name;
            _logger.LogInformation("State {State}: applying command {Command}", ctx.State, cmdLog);
            next = await _engine.HandleAsync(ctx, resp.CommandToApply, ct);

            // Auto-run preflight / submit as a follow-up turn without another user round-trip
            // if the emitted command moves into a terminal-pending state requiring a deterministic call.
            if (resp.CommandToApply is RunPreflightCommand == false &&
                resp.CommandToApply is SubmitCommand == false &&
                next.State == PaState.Preflight)
            {
                // don't auto-run; let user trigger via next turn
            }
        }

        return new OrchestratorTurn(resp.ReplyToUser, next, cmdLog);
    }
}

public record OrchestratorTurn(string Reply, PaWorkflowContext Context, string? CommandApplied);
