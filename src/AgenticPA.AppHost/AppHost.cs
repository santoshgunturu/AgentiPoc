var builder = DistributedApplication.CreateBuilder(args);

// MCP server — exposes tools over HTTP, must be available before Web starts (Web connects on demand).
var mcp = builder.AddProject<Projects.AgenticPA_McpServer>("mcp");

// Web — Blazor + DevUI + A2A + OpenAI Responses + express PA workflow.
var web = builder.AddProject<Projects.AgenticPA_Web>("web")
    .WithReference(mcp)
    .WaitFor(mcp);

// Surface the DevUI + A2A discovery URLs as labelled dashboard links.
web.WithUrls(context =>
{
    var baseUrl = context.Urls.FirstOrDefault();
    if (baseUrl is not null)
    {
        context.Urls.Add(new() { Url = baseUrl.Url.TrimEnd('/') + "/devui",  DisplayText = "DevUI (visual playground)" });
        context.Urls.Add(new() { Url = baseUrl.Url.TrimEnd('/') + "/health", DisplayText = "Health" });
    }
});

builder.Build().Run();
