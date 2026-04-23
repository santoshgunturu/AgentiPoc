using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AgenticPA.Web.Services;

public class LoggingIChatClient : IChatClient
{
    private readonly IChatClient _inner;
    private readonly ChatSessionState _session;

    public LoggingIChatClient(IChatClient inner, ChatSessionState session)
    {
        _inner = inner;
        _session = session;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ChatResponse response = await _inner.GetResponseAsync(messages, options, cancellationToken);
        foreach (ChatMessage m in response.Messages)
        {
            foreach (AIContent c in m.Contents)
            {
                switch (c)
                {
                    case FunctionCallContent call:
                        _session.AppendLog(new SessionLogEntry(
                            DateTime.Now,
                            "tool-call",
                            call.Name,
                            JsonSerializer.Serialize(call.Arguments ?? new Dictionary<string, object?>())));
                        break;
                    case FunctionResultContent res:
                        _session.AppendLog(new SessionLogEntry(
                            DateTime.Now,
                            "tool-result",
                            res.CallId,
                            Describe(res.Result)));
                        break;
                }
            }
        }
        return response;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => _inner.GetStreamingResponseAsync(messages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) => _inner.GetService(serviceType, serviceKey);

    public void Dispose() => _inner.Dispose();

    private static string Describe(object? result)
    {
        if (result is null) return "(null)";
        try
        {
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return result.ToString() ?? "(unprintable)";
        }
    }
}
