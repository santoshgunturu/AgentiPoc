using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class MemberSkill : SkillBase
{
    public MemberSkill(IChatClient chat, McpToolClient mcp, ILogger<MemberSkill> logger, SkillRubricLoader rubricLoader)
        : base(chat, mcp, logger, rubricLoader) { }

    public override PaState Handles => PaState.MemberPending;

    protected override string RubricFileName => "member-search-rubric.md";

    protected override string[] AllowedTools => new[]
    {
        "search_members", "get_member_context",
        "search_client_specific_members", "get_member_enrollments",
        "search_anthem_bc_enrollments", "get_clients"
    };

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        if (json.GetProperty("action").GetString() == "set_member"
            && json.TryGetProperty("memberId", out JsonElement mid))
        {
            string? id = mid.GetString();
            return string.IsNullOrWhiteSpace(id) ? null : new SetMemberCommand(id);
        }
        return null;
    }
}
