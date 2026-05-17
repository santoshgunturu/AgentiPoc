using System.Text.Json;
using AgenticPA.Agent.StateMachine;
using AgenticPA.Services.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AgenticPA.Agent.Mcp;

public class McpToolClient : IAsyncDisposable, IRulesEngineClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly Uri _endpoint;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpToolClient> _logger;
    private McpClient? _client;
    private IReadOnlyList<McpClientTool>? _tools;

    private readonly ToolApprovalPolicy? _approvalPolicy;

    public McpToolClient(Uri endpoint, ILoggerFactory loggerFactory, ToolApprovalPolicy? approvalPolicy = null)
    {
        _endpoint = endpoint;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<McpToolClient>();
        _approvalPolicy = approvalPolicy;
    }

    public IReadOnlyList<AIFunction> AllTools => (IReadOnlyList<AIFunction>?)_tools ?? Array.Empty<AIFunction>();

    /// <summary>
    /// Return the named tools, wrapping any that the approval policy flags so
    /// the LLM cannot invoke them without an explicit human approval.
    /// </summary>
    public IReadOnlyList<AIFunction> ToolsNamed(params string[] names)
    {
        HashSet<string> allow = new(names, StringComparer.OrdinalIgnoreCase);
        List<AIFunction> result = new();
        foreach (AIFunction tool in AllTools.Where(t => allow.Contains(t.Name)))
        {
            if (_approvalPolicy is not null && _approvalPolicy.ShouldApprove(tool.Name))
            {
                result.Add(new ApprovalRequiredAIFunction(tool));
            }
            else
            {
                result.Add(tool);
            }
        }
        return result;
    }

    public async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (_client is not null) return;

        HttpClientTransportOptions options = new()
        {
            Endpoint = _endpoint,
            TransportMode = HttpTransportMode.AutoDetect,
            Name = "AgenticPA"
        };
        HttpClientTransport transport = new(options, _loggerFactory);
        _client = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: ct);
        IList<McpClientTool> tools = await _client.ListToolsAsync(cancellationToken: ct);
        _tools = tools.ToList();
        _logger.LogInformation("Connected to MCP server at {Endpoint}; discovered {Count} tools", _endpoint, _tools.Count);
    }

    public async Task<RulesEvaluation> PreviewAsync(CanonicalPaRequest request, CancellationToken ct)
        => await InvokeRulesToolAsync("preview_criteria_evaluation", request, ct);

    public async Task<RulesEvaluation> SubmitAsync(CanonicalPaRequest request, CancellationToken ct)
        => await InvokeRulesToolAsync("submit_pa", request, ct);

    private async Task<RulesEvaluation> InvokeRulesToolAsync(string toolName, CanonicalPaRequest request, CancellationToken ct)
    {
        await EnsureConnectedAsync(ct);
        McpClientTool tool = _tools!.First(t => t.Name == toolName);
        Dictionary<string, object?> args = new()
        {
            ["canonicalRequest"] = request
        };
        CallToolResult result = await tool.CallAsync(args, cancellationToken: ct);
        string text = string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));
        RulesEvaluation? eval = JsonSerializer.Deserialize<RulesEvaluation>(text, JsonOptions);
        return eval ?? throw new InvalidOperationException($"Tool {toolName} returned unparseable result: {text}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
    }
}
