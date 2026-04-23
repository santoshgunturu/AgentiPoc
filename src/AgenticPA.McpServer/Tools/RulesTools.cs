using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class RulesTools
{
    [McpServerTool(Name = "preview_criteria_evaluation")]
    [Description("Dry-run the deterministic rules engine against a canonical PA request. Returns outcome, gaps, explanation. Does NOT submit.")]
    public static async Task<RulesEvaluation> PreviewCriteriaEvaluation(
        IRulesEngine rules,
        [Description("Canonical PA request payload")] CanonicalPaRequest canonicalRequest)
        => await rules.PreviewAsync(canonicalRequest);

    [McpServerTool(Name = "submit_pa")]
    [Description("Submit the canonical PA request. Runs the deterministic rules engine and returns the final outcome.")]
    public static async Task<RulesEvaluation> SubmitPa(
        IRulesEngine rules,
        [Description("Canonical PA request payload")] CanonicalPaRequest canonicalRequest)
        => await rules.SubmitAsync(canonicalRequest);
}
