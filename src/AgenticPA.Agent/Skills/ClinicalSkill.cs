using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class ClinicalSkill : SkillBase
{
    public ClinicalSkill(IChatClient chat, McpToolClient mcp, ILogger<ClinicalSkill> logger, SkillRubricLoader rubricLoader)
        : base(chat, mcp, logger, rubricLoader) { }

    public override PaState Handles => PaState.ClinicalPending;

    protected override string RubricFileName => "clinical-context-rubric.md";

    protected override string[] AllowedTools => new[]
    {
        "search_diagnosis_codes",
        "search_icd10_hierarchy", "validate_icd_procedure_pairing"
    };

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        if (json.GetProperty("action").GetString() != "set_clinical") return null;
        string? icd = json.TryGetProperty("icd10", out JsonElement e1) ? e1.GetString() : null;
        int weeks = json.TryGetProperty("weeksPt", out JsonElement e2) && e2.ValueKind == JsonValueKind.Number
            ? e2.GetInt32() : 0;
        string notes = json.TryGetProperty("notes", out JsonElement e3) ? (e3.GetString() ?? string.Empty) : string.Empty;
        if (string.IsNullOrWhiteSpace(icd)) return null;
        return new SetClinicalCommand(icd, weeks, notes);
    }
}
