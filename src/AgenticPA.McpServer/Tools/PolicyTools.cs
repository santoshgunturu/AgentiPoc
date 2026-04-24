using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class PolicyTools
{
    [McpServerTool(Name = "get_policy_text")]
    [Description("Return the policy (citation, version, full text) for a CPT, optionally filtered by plan and date.")]
    public static async Task<Policy?> GetPolicyText(
        IPolicyService policies,
        [Description("CPT code")] string cpt,
        [Description("Optional plan id")] string? planId = null,
        [Description("Optional as-of date yyyy-MM-dd")] string? asOf = null)
        => await policies.GetAsync(cpt, planId, asOf);
}
