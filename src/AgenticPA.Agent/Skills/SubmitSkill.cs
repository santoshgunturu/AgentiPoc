using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class SubmitSkill : SkillBase
{
    public SubmitSkill(IChatClient chat, McpToolClient mcp, ILogger<SubmitSkill> logger, SkillRubricLoader rubricLoader, InFlightCounter? inFlight = null)
        : base(chat, mcp, logger, rubricLoader, inFlight) { }

    public override PaState Handles => PaState.Submit;

    protected override string RubricFileName => "submit-rubric.md";

    protected override string[] AllowedTools => new[] { "audit_submission" };

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        string? action = json.GetProperty("action").GetString();
        return action == "submit" ? new SubmitCommand() : null;
    }
}
