using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class ProcedureTools
{
    [McpServerTool(Name = "search_procedure_codes")]
    [Description("Search CPT procedure codes by code, description, or body part.")]
    public static async Task<IReadOnlyList<Procedure>> SearchProcedureCodes(
        IProcedureService procedures,
        [Description("Query: CPT code, description fragment, or body part")] string query)
        => await procedures.SearchAsync(query);

    [McpServerTool(Name = "check_auth_required")]
    [Description("Return true if the given CPT code requires prior authorization.")]
    public static async Task<bool> CheckAuthRequired(
        IProcedureService procedures,
        [Description("CPT code, e.g. 73721")] string cpt)
        => await procedures.CheckAuthRequiredAsync(cpt);

    [McpServerTool(Name = "get_procedure_rules")]
    [Description("Return contraindications, prerequisites, alternatives, and expected duration for a CPT.")]
    public static async Task<ProcedureRule?> GetProcedureRules(
        IProcedureService procedures,
        [Description("CPT code")] string cpt)
        => await procedures.GetProcedureRulesAsync(cpt);

    [McpServerTool(Name = "check_procedure_coverage")]
    [Description("Check whether a CPT is covered under a plan. Returns: covered | requires-pa | not-covered.")]
    public static async Task<string> CheckProcedureCoverage(
        IProcedureService procedures,
        [Description("CPT code")] string cpt,
        [Description("Plan id, e.g. P001")] string planId)
        => await procedures.CheckCoverageAsync(cpt, planId);
}
