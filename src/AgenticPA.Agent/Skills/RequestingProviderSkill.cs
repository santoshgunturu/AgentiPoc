using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class RequestingProviderSkill : SkillBase
{
    public RequestingProviderSkill(IChatClient chat, McpToolClient mcp, ILogger<RequestingProviderSkill> logger, SkillRubricLoader rubricLoader)
        : base(chat, mcp, logger, rubricLoader) { }

    public override PaState Handles => PaState.ReqProviderPending;

    protected override string RubricFileName => "provider-search-rubric.md";

    protected override string[] AllowedTools => new[]
    {
        "search_providers", "get_network_status",
        "get_provider_credentials", "verify_provider_network"
    };

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        if (json.GetProperty("action").GetString() == "set_requesting_provider"
            && json.TryGetProperty("npi", out JsonElement npi))
        {
            string? n = npi.GetString();
            return string.IsNullOrWhiteSpace(n) ? null : new SetRequestingProviderCommand(n);
        }
        return null;
    }
}
