using AgenticPA.Agent;
using AgenticPA.Web.Components;
using AgenticPA.Web.Services;
using Microsoft.Extensions.AI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string endpoint = builder.Configuration["Agent:Endpoint"] ?? "https://api.openai.com/v1";
string model = builder.Configuration["Agent:Model"] ?? "gpt-4o-mini";
string apiKey = builder.Configuration["Agent:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? "missing";
string mcpUrl = builder.Configuration["Mcp:Url"] ?? "http://localhost:7070/";

builder.Services.AddScoped<ChatSessionState>();

builder.Services.AddAgenticPaAgent(
    chatClientFactory: sp =>
    {
        IChatClient baseClient = ServiceCollectionExtensions.BuildOpenAiChatClient(endpoint, model, apiKey);
        ChatSessionState session = sp.GetRequiredService<ChatSessionState>();
        IChatClient withTools = new ChatClientBuilder(baseClient)
            .UseFunctionInvocation()
            .Use(innerClient => new LoggingIChatClient(innerClient, session))
            .Build();
        return withTools;
    },
    mcpEndpointFactory: _ => new Uri(mcpUrl));

builder.Services.AddRubricNightlyRefresh(builder.Configuration);

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
