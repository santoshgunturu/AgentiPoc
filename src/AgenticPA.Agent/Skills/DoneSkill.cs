using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class DoneSkill : SkillBase
{
    public DoneSkill(IChatClient chat, McpToolClient mcp, ILogger<DoneSkill> logger, SkillRubricLoader? rubricLoader = null)
        : base(chat, mcp, logger, rubricLoader) { }

    public override PaState Handles => PaState.Done;

    protected override string[] AllowedTools => Array.Empty<string>();

    protected override string SystemPrompt => """
    You are the Done skill. The PA has been submitted and the rules engine has already produced a final outcome.
    Restate the final outcome clearly with a markdown bullet list:
        Final decision:
        - Outcome: auto-approve
        - Gaps: none
        - Notes: request meets all criteria for CPT 73721
        - Policy: BlueCare PPO §4.2.1 — Advanced Imaging of the Knee
        - Rule: MRI of the knee requires 6 weeks of conservative treatment...
    Include the Policy and Rule lines ONLY if policyCitation/policyText are present in the context hint.
    If the user asks follow-up questions, keep them short. Always emit {"action":"none"}.

    End every reply with EXACTLY ONE JSON command block on its own line (triple-backtick fences):
      ```json
      {"action":"none"}
      ```
    Never emit more than one JSON block.
    """;

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
