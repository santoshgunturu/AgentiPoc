namespace AgenticPA.Agent.Mcp;

/// <summary>
/// Per-tool approval requirement. Tools listed here are wrapped in
/// <c>ApprovalRequiredAIFunction</c> so the LLM cannot invoke them without
/// an explicit human approval round-trip.
///
/// Defaults intentionally err on the safe side for healthcare PA workflows.
/// </summary>
public class ToolApprovalPolicy
{
    /// <summary>Names of MCP tools that REQUIRE explicit human approval before execution.</summary>
    public HashSet<string> RequireApproval { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "submit_pa",         // state-changing — writes the final PA decision
        "audit_submission"   // state-changing — writes the audit record
    };

    public bool ShouldApprove(string toolName) => RequireApproval.Contains(toolName);
}
