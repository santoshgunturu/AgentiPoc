using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public abstract class SkillBase : ISkill
{
    protected readonly IChatClient ChatClient;
    protected readonly McpToolClient Mcp;
    protected readonly ILogger Logger;
    protected readonly SkillRubricLoader? RubricLoader;

    protected SkillBase(IChatClient chatClient, McpToolClient mcp, ILogger logger, SkillRubricLoader? rubricLoader = null)
    {
        ChatClient = chatClient;
        Mcp = mcp;
        Logger = logger;
        RubricLoader = rubricLoader;
    }

    public abstract PaState Handles { get; }

    /// <summary>
    /// Optional rubric file (in skills/*.md). If set and the file loads, the rubric
    /// replaces the hardcoded SystemPrompt. Falls back to SystemPrompt otherwise.
    /// </summary>
    protected virtual string? RubricFileName => null;

    protected abstract string SystemPrompt { get; }
    protected abstract string[] AllowedTools { get; }

    protected string ResolveSystemPrompt()
    {
        if (RubricFileName is not null && RubricLoader is not null)
        {
            string rubric = RubricLoader.Load(RubricFileName);
            if (!string.IsNullOrWhiteSpace(rubric) && rubric.Length > 200)
            {
                return rubric + "\n\n---\n\n" + SystemPrompt;
            }
        }
        return SystemPrompt;
    }

    public async Task<SkillResponse> HandleTurnAsync(
        PaWorkflowContext ctx,
        IReadOnlyList<ChatTurn> transcript,
        string userMessage,
        CancellationToken ct)
    {
        await Mcp.EnsureConnectedAsync(ct);

        List<ChatMessage> messages = new()
        {
            new ChatMessage(ChatRole.System, ResolveSystemPrompt() + "\n\n" + ContextHint(ctx))
        };
        foreach (ChatTurn turn in transcript)
        {
            if (turn.Role == "coach") continue;
            messages.Add(new ChatMessage(
                turn.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                turn.Content));
        }
        messages.Add(new ChatMessage(ChatRole.User, userMessage));

        ChatOptions options = new()
        {
            Tools = AllowedTools.Length == 0
                ? null
                : Mcp.ToolsNamed(AllowedTools).Cast<AITool>().ToList()
        };

        ChatResponse response = await ChatClient.GetResponseAsync(messages, options, cancellationToken: ct);
        string fullText = response.Text ?? string.Empty;

        (string reply, IWorkflowCommand? cmd) = ExtractCommand(fullText, ctx);
        return new SkillResponse(reply, cmd);
    }

    protected virtual string ContextHint(PaWorkflowContext ctx) =>
        $"Current workflow state: {ctx.State}. Accumulated slots: member={ctx.MemberId ?? "?"}, cpt={ctx.Cpt ?? "?"}, provider={ctx.RequestingNpi ?? "?"}, facility={ctx.FacilityNpi ?? "?"}, icd10={ctx.Icd10 ?? "?"}, ptWeeks={ctx.ConservativeTreatmentWeeks?.ToString() ?? "?"}.";

    protected abstract IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx);

    private (string reply, IWorkflowCommand? cmd) ExtractCommand(string raw, PaWorkflowContext ctx)
    {
        string working = raw;
        IWorkflowCommand? commandToApply = null;

        // Strip every {"action":...} object in the message, keeping the LAST non-"none" one as the command.
        while (true)
        {
            (string? body, int start, int end) = FindActionJson(working);
            if (body is null) break;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("action", out JsonElement action))
                {
                    string? actionName = action.GetString();
                    if (!string.IsNullOrWhiteSpace(actionName)
                        && !actionName.Equals("none", StringComparison.OrdinalIgnoreCase)
                        && commandToApply is null)
                    {
                        commandToApply = ParseCommand(doc.RootElement, ctx);
                    }
                }
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Skill produced invalid JSON command block: {Body}", body);
            }

            working = StripRange(working, start, end);
        }

        return (working.Trim(), commandToApply);
    }

    // Locate the last balanced top-level JSON object that has an "action" key, and also
    // expand the strip range to cover optional fences/labels like ```json / ``` / bare "json".
    private static (string? body, int start, int end) FindActionJson(string raw)
    {
        for (int i = raw.Length - 1; i >= 0; i--)
        {
            if (raw[i] != '}') continue;
            int end = i;
            int depth = 0;
            for (int j = end; j >= 0; j--)
            {
                char c = raw[j];
                if (c == '}') depth++;
                else if (c == '{')
                {
                    depth--;
                    if (depth == 0)
                    {
                        string candidate = raw.Substring(j, end - j + 1);
                        if (candidate.Contains("\"action\"", StringComparison.OrdinalIgnoreCase))
                        {
                            int stripStart = ExpandStripStart(raw, j);
                            int stripEnd = ExpandStripEnd(raw, end + 1);
                            return (candidate, stripStart, stripEnd);
                        }
                        break;
                    }
                }
            }
        }
        return (null, 0, 0);
    }

    // Walk backwards from the `{` past any combination of whitespace, markdown fence tokens
    // (```), and bare language labels ("json"), skipping lines that contain only those.
    private static int ExpandStripStart(string raw, int jsonStart)
    {
        int result = jsonStart;
        int k = jsonStart - 1;
        while (k >= 0)
        {
            int lineEnd = k;
            while (lineEnd >= 0 && raw[lineEnd] != '\n') lineEnd--;
            int lineStart = lineEnd + 1;
            int segEnd = k;
            string line = raw.Substring(lineStart, segEnd - lineStart + 1).Trim();

            if (line.Length == 0 || IsFenceOrLabel(line))
            {
                result = lineStart;
                k = lineEnd - 1;
                continue;
            }
            break;
        }
        return result;
    }

    private static int ExpandStripEnd(string raw, int jsonEnd)
    {
        int result = jsonEnd;
        int k = jsonEnd;
        while (k < raw.Length)
        {
            int lineStart = k;
            int lineEnd = lineStart;
            while (lineEnd < raw.Length && raw[lineEnd] != '\n') lineEnd++;
            string line = raw.Substring(lineStart, lineEnd - lineStart).Trim();

            if (line.Length == 0 || IsFenceOrLabel(line))
            {
                result = lineEnd < raw.Length ? lineEnd + 1 : lineEnd;
                k = result;
                continue;
            }
            break;
        }
        return result;
    }

    private static bool IsFenceOrLabel(string line)
    {
        string t = line.Trim();
        if (t.Length == 0) return true;
        if (t.StartsWith("```"))
        {
            string rest = t.Substring(3).Trim();
            return rest.Length == 0 || rest.Equals("json", StringComparison.OrdinalIgnoreCase);
        }
        return t.Equals("json", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripRange(string raw, int start, int end)
    {
        string before = raw.Substring(0, start);
        string after = end < raw.Length ? raw.Substring(end) : string.Empty;
        return (before + after).TrimEnd().TrimStart();
    }
}
