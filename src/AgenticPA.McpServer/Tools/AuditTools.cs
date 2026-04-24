using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class AuditTools
{
    [McpServerTool(Name = "audit_submission")]
    [Description("Record an audit entry for a submitted PA. Returns audit case id.")]
    public static async Task<AuditRecord> AuditSubmission(
        IAuditService audit,
        [Description("Canonical PA request payload")] CanonicalPaRequest canonicalRequest,
        [Description("Outcome: auto-approve, pend, or deny")] string outcome)
        => await audit.RecordAsync(canonicalRequest, outcome);
}
