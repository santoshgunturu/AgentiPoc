using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class SubmitSkill : SkillBase
{
    public SubmitSkill(IChatClient chat, McpToolClient mcp, ILogger<SubmitSkill> logger, SkillRubricLoader? rubricLoader = null)
        : base(chat, mcp, logger, rubricLoader) { }

    public override PaState Handles => PaState.Submit;

    protected override string? RubricFileName => "submit-rubric.md";

    protected override string[] AllowedTools => new[] { "audit_submission" };

    protected override string SystemPrompt => """
    You are the Submit skill. The user has seen the preflight outcome and is deciding whether to submit.
    Rules:
      - If the user says anything affirmative (yes, submit, go ahead, proceed), emit a submit command.
      - If the user wants to change anything (e.g. "actually I did 8 weeks of PT"), emit "none" and
        explain they will need to restart the session (this POC does not support rollback).
      - Never invent a rules-engine verdict. You do not make the medical decision.

    End every reply with EXACTLY ONE JSON command block on its own line (triple-backtick fences).
    Choose exactly one:
      (a) when the user confirms submission:
          ```json
          {"action":"submit"}
          ```
      (b) otherwise:
          ```json
          {"action":"none"}
          ```
    Never emit more than one JSON block.
    """;

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        string? action = json.GetProperty("action").GetString();
        return action == "submit" ? new SubmitCommand() : null;
    }
}
