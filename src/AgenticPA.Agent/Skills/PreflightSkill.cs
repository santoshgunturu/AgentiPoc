using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class PreflightSkill : SkillBase
{
    public PreflightSkill(IChatClient chat, McpToolClient mcp, ILogger<PreflightSkill> logger)
        : base(chat, mcp, logger) { }

    public override PaState Handles => PaState.Preflight;

    protected override string[] AllowedTools => new[] { "preview_criteria_evaluation" };

    protected override string SystemPrompt => """
    You are the Pre-flight skill. You explain the deterministic rules-engine dry-run outcome
    to the user in plain English before they submit.
    Rules:
      - If no preflightResult is present in the context hint yet, emit a run_preflight command so
        the state machine invokes the rules engine.
      - Once preflightResult IS present, describe outcome (auto-approve / pend / deny) and any gaps
        in plain English. Use a markdown bullet list so the operator can verify:
            Pre-flight result:
            - Outcome: auto-approve
            - Gaps: none
            - Notes: request meets all criteria for CPT 73721
            - Policy: BlueCare PPO §4.2.1 — Advanced Imaging of the Knee
            - Rule: MRI of the knee requires 6 weeks of conservative treatment...
        Include the Policy and Rule lines ONLY if policyCitation/policyText are present in the context hint.
        Then ask the user if they want to submit.
      - Emit a "none" action after explaining — the Submit state will handle user confirmation.

    End every reply with EXACTLY ONE JSON command block on its own line (triple-backtick fences).
    Choose exactly one:
      (a) to invoke the rules-engine dry-run:
          ```json
          {"action":"run_preflight"}
          ```
      (b) after explaining the result:
          ```json
          {"action":"none"}
          ```
    Never emit more than one JSON block.
    """;

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
