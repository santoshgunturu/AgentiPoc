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
    public async Task McpClient_ListsElevenTools()
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

            client.AllTools.Should().HaveCount(11);
            client.AllTools.Select(t => t.Name).Should().Contain(new[]
            {
                "search_members", "get_member_context",
                "search_procedure_codes", "check_auth_required",
                "search_providers", "get_network_status",
                "search_facilities", "validate_pos_for_cpt",
                "search_diagnosis_codes",
                "preview_criteria_evaluation", "submit_pa"
            });
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
