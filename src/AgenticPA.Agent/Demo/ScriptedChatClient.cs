using Microsoft.Extensions.AI;

namespace AgenticPA.Agent.Demo;

/// <summary>
/// IChatClient that returns scripted, deterministic responses — no LLM call.
/// Used for demos on machines where outbound LLM access is not yet approved.
/// Emits FunctionCallContent so UseFunctionInvocation still drives real MCP tool
/// calls, then a final reply with the same {"action":...} JSON shape the skills
/// already parse.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        List<ChatMessage> list = messages.ToList();
        string skill = DetectSkill(list);
        string user = LastUserMessage(list);
        int toolResultCount = list.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Count();
        IReadOnlyList<string> availableTools = options?.Tools?.OfType<AIFunction>().Select(t => t.Name).ToList()
            ?? (IReadOnlyList<string>)Array.Empty<string>();

        ChatMessage reply = DemoScript.Build(skill, user, toolResultCount, availableTools);
        return Task.FromResult(new ChatResponse(reply));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("ScriptedChatClient does not support streaming.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }

    private static string DetectSkill(List<ChatMessage> messages)
    {
        string system = messages.FirstOrDefault(m => m.Role == ChatRole.System)?.Text ?? string.Empty;
        if (system.Contains("member-search-conversational-skill")) return "member";
        if (system.Contains("procedure-search-conversational-skill")) return "procedure";
        if (system.Contains("provider-search-conversational-skill")) return "provider";
        if (system.Contains("facility-search-conversational-skill")) return "facility";
        if (system.Contains("clinical-context-conversational-skill")) return "clinical";
        if (system.Contains("preflight-conversational-skill")) return "preflight";
        if (system.Contains("submit-conversational-skill")) return "submit";
        if (system.Contains("done-conversational-skill")) return "done";
        return "unknown";
    }

    private static string LastUserMessage(List<ChatMessage> messages) =>
        messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
}
