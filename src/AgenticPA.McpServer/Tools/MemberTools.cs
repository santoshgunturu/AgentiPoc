using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class MemberTools
{
    [McpServerTool(Name = "search_members")]
    [Description("Search members by id, name, or dob. Returns matching members. Use DOB to disambiguate.")]
    public static async Task<IReadOnlyList<Member>> SearchMembers(
        IMemberService members,
        [Description("Free-text query: member id, partial name, or DOB in yyyy-MM-dd")] string query)
        => await members.SearchAsync(query);

    [McpServerTool(Name = "get_member_context")]
    [Description("Return the full context for a member by id (plan, pcp, coverage).")]
    public static async Task<Member?> GetMemberContext(
        IMemberService members,
        [Description("Member id, e.g. M1001")] string memberId)
        => await members.GetContextAsync(memberId);
}
