using A2A.AspNetCore;
using AgenticPA.Agent;
using AgenticPA.Agent.Demo;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.Skills;
using AgenticPA.Agent.Workflows;
using AgenticPA.Services;
using AgenticPA.Web.Components;
using AgenticPA.Web.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.A2A;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Phase 6 — Aspire ServiceDefaults: OTel, resilience, service discovery, health checks.
builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string provider = builder.Configuration["Agent:Provider"] ?? "OpenAI";
string endpoint = builder.Configuration["Agent:Endpoint"] ?? "https://api.openai.com/v1";
string model = builder.Configuration["Agent:Model"] ?? "gpt-4o-mini";
string apiKey = builder.Configuration["Agent:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? "missing";
string mcpUrl = builder.Configuration["Mcp:Url"] ?? "http://localhost:7070/";

builder.Services.AddScoped<ChatSessionState>();

// Session persistence (Phase 4) — in-memory for dev, swap for Cosmos/Redis in prod.
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

// Phase 3 — register the in-process rules engine + audit so the express PA endpoint can resolve them.
builder.Services.AddAgenticPaServices();

builder.Services.AddAgenticPaAgent(
    chatClientFactory: sp =>
    {
        IChatClient baseClient = provider.Equals("Demo", StringComparison.OrdinalIgnoreCase)
            ? new ScriptedChatClient()
            : AgenticPA.Agent.ServiceCollectionExtensions.BuildOpenAiChatClient(endpoint, model, apiKey);
        ChatSessionState session = sp.GetRequiredService<ChatSessionState>();
        IChatClient withTools = new ChatClientBuilder(baseClient)
            .UseFunctionInvocation()
            .Use(innerClient => new LoggingIChatClient(innerClient, session))
            .Build();
        return withTools;
    },
    mcpEndpointFactory: _ => new Uri(mcpUrl));

builder.Services.AddRubricNightlyRefresh(builder.Configuration);

// Phase 5/6 — register a singleton "static" IChatClient with OpenTelemetry GenAI tracing.
builder.Services.AddSingleton<IChatClient>(sp =>
{
    IChatClient baseClient = provider.Equals("Demo", StringComparison.OrdinalIgnoreCase)
        ? new ScriptedChatClient()
        : AgenticPA.Agent.ServiceCollectionExtensions.BuildOpenAiChatClient(endpoint, model, apiKey);
    bool isDev = builder.Environment.IsDevelopment();
    return new ChatClientBuilder(baseClient)
        .UseFunctionInvocation()
        .UseOpenTelemetry(configure: c => c.EnableSensitiveData = isDev)
        .Build();
});

// Phase 5 — register each skill as a MAF AIAgent for DevUI / OpenAI Responses / A2A discovery.
builder.AddPaIntakeAgents();

// Phase 5 — OpenAI-compatible REST surface (Responses + Conversations).
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// Phase 5 — DevUI (visual playground at /devui).
builder.Services.AddDevUI();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Phase 5 — map multi-surface endpoints.
app.MapDevUI();
app.MapOpenAIResponses();
app.MapOpenAIConversations();

// Phase 5 — A2A: expose each named agent at /a2a/<name>. The .AddA2AServer() call
// during AddAIAgent registration (see MafAgentRegistration) already wired the server;
// MapA2AHttpJson just publishes the HTTP route.
foreach (var (agentName, _) in MafAgentRegistration.AgentRubricMap)
{
    AIAgent agent = app.Services.GetRequiredKeyedService<AIAgent>(agentName);
    app.MapA2AHttpJson(agent, $"/a2a/{agentName.ToLowerInvariant().Replace("agent", "")}");
}

// Phase 3 — express PA workflow API endpoint (single-shot, no operator interaction).
app.MapPost("/api/express-pa", async (PaExpressState request,
    IRulesEngine rules, IAuditService audit) =>
{
    Workflow workflow = PaExpressWorkflow.Build(rules, audit);
    PaExpressState? final = null;
    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, request);
    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        if (evt is ExecutorCompletedEvent ev && ev.Data is PaExpressState s) final = s;
    }
    return Results.Ok(final);
});

app.Run();
