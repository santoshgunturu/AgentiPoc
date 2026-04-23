using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.Skills;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace AgenticPA.Agent;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgenticPaAgent(
        this IServiceCollection services,
        Func<IServiceProvider, IChatClient> chatClientFactory,
        Func<IServiceProvider, Uri> mcpEndpointFactory)
    {
        services.AddScoped(sp => chatClientFactory(sp));

        services.AddSingleton(sp =>
        {
            ILoggerFactory lf = sp.GetRequiredService<ILoggerFactory>();
            return new McpToolClient(mcpEndpointFactory(sp), lf);
        });
        services.AddSingleton<IRulesEngineClient>(sp => sp.GetRequiredService<McpToolClient>());

        services.AddScoped<PaWorkflowEngine>();
        services.AddScoped<ISkill, MemberSkill>();
        services.AddScoped<ISkill, ProcedureSkill>();
        services.AddScoped<ISkill, RequestingProviderSkill>();
        services.AddScoped<ISkill, FacilitySkill>();
        services.AddScoped<ISkill, ClinicalSkill>();
        services.AddScoped<ISkill, PreflightSkill>();
        services.AddScoped<ISkill, SubmitSkill>();
        services.AddScoped<ISkill, DoneSkill>();
        services.AddScoped<Orchestrator>();
        return services;
    }

    public static IChatClient BuildOpenAiChatClient(string endpoint, string model, string apiKey)
    {
        OpenAIClientOptions options = new() { Endpoint = new Uri(endpoint) };
        OpenAIClient client = new(new System.ClientModel.ApiKeyCredential(apiKey), options);
        return client.GetChatClient(model).AsIChatClient();
    }
}
