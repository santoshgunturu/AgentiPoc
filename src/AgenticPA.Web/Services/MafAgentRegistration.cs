using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.Skills;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.A2A;
using Microsoft.Extensions.AI;

namespace AgenticPA.Web.Services;

/// <summary>
/// Registers each skill's rubric-driven prompt as a named <see cref="AIAgent"/>
/// in DI so DevUI, OpenAI Responses, and A2A can discover and invoke it.
///
/// Note: these are singletons built at startup, so dev-mode rubric reloads do
/// NOT affect them. The interactive Blazor flow continues to use <c>SkillBase</c>
/// with live reload. These registrations are the "testing / interop" surface.
/// </summary>
public static class MafAgentRegistration
{
    /// <summary>Maps a friendly agent name to its rubric file name.</summary>
    public static readonly IReadOnlyDictionary<string, string> AgentRubricMap = new Dictionary<string, string>
    {
        ["MemberAgent"]            = "member-search-rubric.md",
        ["ProcedureAgent"]         = "procedure-search-rubric.md",
        ["ProviderAgent"]          = "provider-search-rubric.md",
        ["FacilityAgent"]          = "facility-search-rubric.md",
        ["ClinicalAgent"]          = "clinical-context-rubric.md",
        ["PreflightAgent"]         = "preflight-rubric.md",
        ["SubmitAgent"]            = "submit-rubric.md"
    };

    public static IHostApplicationBuilder AddPaIntakeAgents(
        this IHostApplicationBuilder builder)
    {
        foreach (var (agentName, rubricFile) in AgentRubricMap)
        {
            string capturedName = agentName;
            string capturedRubric = rubricFile;

            builder.AddAIAgent(capturedName, (sp, _) =>
            {
                IChatClient chatClient = sp.GetRequiredService<IChatClient>();
                SkillRubricLoader rubricLoader = sp.GetRequiredService<SkillRubricLoader>();
                McpToolClient mcp = sp.GetRequiredService<McpToolClient>();

                string instructions = rubricLoader.Load(capturedRubric);
                IList<AITool>? tools = ResolveToolsForAgent(capturedName, mcp);

                return new ChatClientAgent(
                    chatClient: chatClient,
                    name: capturedName,
                    description: $"Rubric-driven PA workflow agent: {capturedName}",
                    instructions: instructions,
                    tools: tools,
                    loggerFactory: null,
                    services: null);
            }).AddA2AServer();
        }

        return builder;
    }

    private static IList<AITool>? ResolveToolsForAgent(string agentName, McpToolClient mcp) => agentName switch
    {
        "MemberAgent"     => mcp.ToolsNamed("search_members", "get_member_context",
                                            "search_client_specific_members", "get_member_enrollments",
                                            "search_anthem_bc_enrollments", "get_clients").Cast<AITool>().ToList(),
        "ProcedureAgent"  => mcp.ToolsNamed("search_procedure_codes", "check_auth_required",
                                            "get_procedure_rules", "check_procedure_coverage").Cast<AITool>().ToList(),
        "ProviderAgent"   => mcp.ToolsNamed("search_providers", "get_network_status",
                                            "get_provider_credentials", "verify_provider_network").Cast<AITool>().ToList(),
        "FacilityAgent"   => mcp.ToolsNamed("search_facilities", "validate_pos_for_cpt",
                                            "get_facility_certifications", "validate_facility_for_procedure").Cast<AITool>().ToList(),
        "ClinicalAgent"   => mcp.ToolsNamed("search_diagnosis_codes",
                                            "search_icd10_hierarchy", "validate_icd_procedure_pairing").Cast<AITool>().ToList(),
        "PreflightAgent"  => mcp.ToolsNamed("preview_criteria_evaluation", "get_policy_text").Cast<AITool>().ToList(),
        "SubmitAgent"     => mcp.ToolsNamed("audit_submission").Cast<AITool>().ToList(),
        _                 => null
    };
}
