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

    [McpServerTool(Name = "search_client_specific_members")]
    [Description("Search members scoped to a health plan OR state. Use when you know the plan or issuing state.")]
    public static async Task<IReadOnlyList<Member>> SearchClientSpecificMembers(
        IMemberService members,
        [Description("Optional first name")] string? firstName = null,
        [Description("Optional last name")] string? lastName = null,
        [Description("Optional DOB in yyyy-MM-dd")] string? dob = null,
        [Description("Optional member id")] string? memberId = null,
        [Description("Health plan name (e.g. BlueCare PPO) OR state code (e.g. FL)")] string? healthPlanOrState = null)
        => await members.SearchClientSpecificAsync(firstName, lastName, dob, memberId, healthPlanOrState);

    [McpServerTool(Name = "get_member_enrollments")]
    [Description("Return enrollment periods for a member. Filter by dateOfService if provided.")]
    public static async Task<IReadOnlyList<MemberEnrollment>> GetMemberEnrollments(
        IMemberService members,
        [Description("Member id")] string memberId,
        [Description("Optional date of service in yyyy-MM-dd")] string? dateOfService = null)
        => await members.GetEnrollmentsAsync(memberId, dateOfService);

    [McpServerTool(Name = "search_anthem_bc_enrollments")]
    [Description("Search the Anthem BC enrollment system (special handling). Only for Anthem BC clients.")]
    public static async Task<IReadOnlyList<AnthemBcEnrollment>> SearchAnthemBcEnrollments(
        IMemberService members,
        [Description("Member id")] string memberId,
        [Description("Anthem BC client id (e.g. C002)")] string clientId)
        => await members.SearchAnthemBcEnrollmentsAsync(memberId, clientId);
}
