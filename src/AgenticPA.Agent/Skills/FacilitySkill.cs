using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class FacilitySkill : SkillBase
{
    public FacilitySkill(IChatClient chat, McpToolClient mcp, ILogger<FacilitySkill> logger, SkillRubricLoader rubricLoader)
        : base(chat, mcp, logger, rubricLoader) { }

    public override PaState Handles => PaState.FacilityPending;

    protected override string RubricFileName => "facility-search-rubric.md";

    protected override string[] AllowedTools => new[]
    {
        "search_facilities", "validate_pos_for_cpt",
        "get_facility_certifications", "validate_facility_for_procedure"
    };

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        if (json.GetProperty("action").GetString() == "set_facility"
            && json.TryGetProperty("facilityNpi", out JsonElement npi))
        {
            string? n = npi.GetString();
            return string.IsNullOrWhiteSpace(n) ? null : new SetFacilityCommand(n);
        }
        return null;
    }
}
