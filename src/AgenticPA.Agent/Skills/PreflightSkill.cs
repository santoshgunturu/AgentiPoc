using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class PreflightSkill : SkillBase
{
    public PreflightSkill(IChatClient chat, McpToolClient mcp, ILogger<PreflightSkill> logger, SkillRubricLoader rubricLoader)
        : base(chat, mcp, logger, rubricLoader) { }

    public override PaState Handles => PaState.Preflight;

    protected override string RubricFileName => "preflight-rubric.md";

    protected override string[] AllowedTools => new[] { "preview_criteria_evaluation", "get_policy_text" };

    protected override string ContextHint(PaWorkflowContext ctx)
    {
        string baseline = base.ContextHint(ctx);
        if (ctx.PreflightResult is null) return baseline + " preflightResult=(not yet run)";
        string citation = ctx.PreflightResult.PolicyCitation is null ? string.Empty
            : $", policyCitation=\"{ctx.PreflightResult.PolicyCitation}\", policyText=\"{ctx.PreflightResult.PolicyText}\"";
        return baseline + $" preflightResult=outcome={ctx.PreflightResult.Outcome}, gaps=[{string.Join(",", ctx.PreflightResult.Gaps)}], explanation=\"{ctx.PreflightResult.Explanation}\"{citation}";
    }

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        string? action = json.GetProperty("action").GetString();
        return action == "run_preflight" ? new RunPreflightCommand() : null;
    }
}
