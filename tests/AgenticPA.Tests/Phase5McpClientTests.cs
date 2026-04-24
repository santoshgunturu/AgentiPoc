using AgenticPA.Agent.Mcp;
using AgenticPA.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgenticPA.Tests;

public class Phase5McpClientTests
{
    [Fact]
    public async Task McpClient_ListsAllExpectedTools()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:7071");
        builder.Logging.ClearProviders();
        builder.Services.AddAgenticPaServices();
        builder.Services.AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithToolsFromAssembly(typeof(AgenticPA.McpServer.Tools.MemberTools).Assembly);

        WebApplication app = builder.Build();
        app.MapMcp();

        await app.StartAsync();
        try
        {
            using ILoggerFactory lf = LoggerFactory.Create(b => b.AddDebug());
            await using McpToolClient client = new(new Uri("http://127.0.0.1:7071/"), lf);
            await client.EnsureConnectedAsync();

            string[] expected = new[]
            {
                // Member (5)
                "search_members", "get_member_context",
                "search_client_specific_members", "get_member_enrollments", "search_anthem_bc_enrollments",
                // Procedure (4)
                "search_procedure_codes", "check_auth_required",
                "get_procedure_rules", "check_procedure_coverage",
                // Provider (4)
                "search_providers", "get_network_status",
                "get_provider_credentials", "verify_provider_network",
                // Facility (4)
                "search_facilities", "validate_pos_for_cpt",
                "get_facility_certifications", "validate_facility_for_procedure",
                // Diagnosis (3)
                "search_diagnosis_codes",
                "search_icd10_hierarchy", "validate_icd_procedure_pairing",
                // Rules (2)
                "preview_criteria_evaluation", "submit_pa",
                // Client (1)
                "get_clients",
                // Policy (1)
                "get_policy_text",
                // Audit (1)
                "audit_submission"
            };

            client.AllTools.Should().HaveCountGreaterOrEqualTo(expected.Length);
            client.AllTools.Select(t => t.Name).Should().Contain(expected);
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
