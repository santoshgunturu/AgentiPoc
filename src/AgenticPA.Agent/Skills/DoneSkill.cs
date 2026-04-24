using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class DoneSkill : SkillBase
{
    public DoneSkill(IChatClient chat, McpToolClient mcp, ILogger<DoneSkill> logger, SkillRubricLoader rubricLoader)
        : base(chat, mcp, logger, rubricLoader) { }

    public override PaState Handles => PaState.Done;

    protected override string RubricFileName => "done-rubric.md";

    protected override string[] AllowedTools => Array.Empty<string>();

    protected override string ContextHint(PaWorkflowContext ctx)
    {
        string baseline = base.ContextHint(ctx);
        if (ctx.SubmitResult is null) return baseline;
        string citation = ctx.SubmitResult.PolicyCitation is null ? string.Empty
            : $", policyCitation=\"{ctx.SubmitResult.PolicyCitation}\", policyText=\"{ctx.SubmitResult.PolicyText}\"";
        return baseline + $" submitResult=outcome={ctx.SubmitResult.Outcome}, gaps=[{string.Join(",", ctx.SubmitResult.Gaps)}], explanation=\"{ctx.SubmitResult.Explanation}\"{citation}";
    }

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx) => null;
}
