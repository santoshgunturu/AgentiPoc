using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class ProcedureSkill : SkillBase
{
    public ProcedureSkill(IChatClient chat, McpToolClient mcp, ILogger<ProcedureSkill> logger, SkillRubricLoader rubricLoader, InFlightCounter? inFlight = null)
        : base(chat, mcp, logger, rubricLoader, inFlight) { }

    public override PaState Handles => PaState.ProcedurePending;

    protected override string RubricFileName => "procedure-search-rubric.md";

    protected override string[] AllowedTools => new[]
    {
        "search_procedure_codes", "check_auth_required",
        "get_procedure_rules", "check_procedure_coverage"
    };

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        if (json.GetProperty("action").GetString() == "set_procedure"
            && json.TryGetProperty("cpt", out JsonElement cpt))
        {
            string? code = cpt.GetString();
            return string.IsNullOrWhiteSpace(code) ? null : new SetProcedureCommand(code);
        }
        return null;
    }
}
