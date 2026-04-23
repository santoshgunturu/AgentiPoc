using AgenticPA.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddAgenticPaServices();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

builder.WebHost.UseUrls("http://localhost:7070");

WebApplication app = builder.Build();

app.MapMcp();
app.MapGet("/", () => "AgenticPA MCP server is running. MCP endpoint: /");

app.Run();
