using Microsoft.Extensions.AI;

namespace AgenticPA.Agent.Demo;

/// <summary>
/// Scripted happy-path responses for the no-LLM demo. Each method returns a
/// ChatMessage shaped exactly like an LLM response: optional FunctionCallContent
/// (auto-invoked by UseFunctionInvocation) on the first turn, then plain text
/// with a {"action":...} JSON block that SkillBase.ExtractCommand parses.
/// IDs are real entries from src/AgenticPA.Services/Data so MCP tool calls
/// resolve against actual seed data.
/// </summary>
internal static class DemoScript
{
    public static ChatMessage Build(string skill, string user, int toolResultsSeen, IReadOnlyList<string> availableTools)
    {
        return skill switch
        {
            "member" => Member(user, toolResultsSeen, availableTools),
            "procedure" => Procedure(user, toolResultsSeen, availableTools),
            "provider" => Provider(user, toolResultsSeen, availableTools),
            "facility" => Facility(user, toolResultsSeen, availableTools),
            "clinical" => Clinical(user),
            "preflight" => Preflight(user),
            "submit" => Submit(user),
            "done" => Done(user),
            _ => Fallback()
        };
    }

    // ----- Member -----
    // Branching example: if the user gives only a name without DOB, ask for DOB first.
    private static ChatMessage Member(string user, int toolResults, IReadOnlyList<string> tools)
    {
        bool hasDob = ContainsDate(user);

        if (toolResults == 0 && !hasDob && user.Length > 0)
        {
            // No tool call yet; nudge for DOB.
            return Text(
                "I see a name but no date of birth. To pick the right member, could you share the DOB (yyyy-MM-dd)?",
                Json("\"action\":\"none\""));
        }

        if (toolResults == 0 && tools.Contains("search_members"))
        {
            return ToolCall("search_members", new() { ["query"] = ExtractMemberQuery(user) });
        }

        // After tool results (or if no tool was available), commit.
        return Text(
            "Got it — Jane Smith (DOB 1978-04-12), member M1001 on BlueCare PPO (active).",
            Json("\"action\":\"set_member\",\"memberId\":\"M1001\""));
    }

    // ----- Procedure -----
    private static ChatMessage Procedure(string user, int toolResults, IReadOnlyList<string> tools)
    {
        if (toolResults == 0 && tools.Contains("search_procedure_codes"))
        {
            return ToolCall("search_procedure_codes", new() { ["query"] = string.IsNullOrWhiteSpace(user) ? "MRI knee" : user });
        }

        return Text(
            "MRI lower extremity without contrast (left knee) — CPT 73721. Prior auth required.",
            Json("\"action\":\"set_procedure\",\"cpt\":\"73721\""));
    }

    // ----- Requesting Provider -----
    private static ChatMessage Provider(string user, int toolResults, IReadOnlyList<string> tools)
    {
        if (toolResults == 0 && tools.Contains("search_providers"))
        {
            return ToolCall("search_providers", new() { ["query"] = string.IsNullOrWhiteSpace(user) ? "Ramirez" : user, ["state"] = "FL" });
        }

        return Text(
            "Dr. Elena Ramirez, Orthopedic Surgery, NPI 1111111111, in network (FL).",
            Json("\"action\":\"set_requesting_provider\",\"npi\":\"1111111111\""));
    }

    // ----- Facility -----
    private static ChatMessage Facility(string user, int toolResults, IReadOnlyList<string> tools)
    {
        if (toolResults == 0 && tools.Contains("search_facilities"))
        {
            return ToolCall("search_facilities", new() { ["query"] = string.IsNullOrWhiteSpace(user) ? "Capital Imaging" : user });
        }

        return Text(
            "Capital Imaging Center, NPI 9990001 (POS 22, outpatient hospital, in network).",
            Json("\"action\":\"set_facility\",\"facilityNpi\":\"9990001\""));
    }

    // ----- Clinical -----
    // No tool call needed — the command itself carries ICD-10 + PT weeks + notes.
    private static ChatMessage Clinical(string user)
    {
        return Text(
            "Captured: ICD-10 M17.12 (left primary osteoarthritis of knee), 8 weeks of conservative PT, persistent mechanical symptoms.",
            Json("\"action\":\"set_clinical\",\"icd10\":\"M17.12\",\"weeksPt\":8,\"notes\":\"Failed 8 weeks of conservative PT; persistent mechanical symptoms.\""));
    }

    // ----- Preflight -----
    // The workflow engine runs the rules engine itself when it applies RunPreflightCommand.
    // The skill just needs to emit run_preflight (or, after the engine has run, narrate the result).
    private static ChatMessage Preflight(string user)
    {
        return Text(
            "Running preflight against the deterministic rules engine…",
            Json("\"action\":\"run_preflight\""));
    }

    // ----- Submit -----
    private static ChatMessage Submit(string user)
    {
        return Text(
            "Submitting the prior authorization request to the rules engine for the final decision.",
            Json("\"action\":\"submit\""));
    }

    // ----- Done -----
    // No command; just a reply.
    private static ChatMessage Done(string user)
    {
        return new ChatMessage(
            ChatRole.Assistant,
            "The decision is rendered above. Ask me anything about the rationale, the policy citation, or what to do next.");
    }

    private static ChatMessage Fallback() =>
        new(ChatRole.Assistant, "Demo mode: I don't have a scripted response for this state. " + Json("\"action\":\"none\""));

    // ----- Helpers -----

    private static ChatMessage Text(string narrative, string jsonBlock) =>
        new(ChatRole.Assistant, narrative + "\n\n```json\n{" + jsonBlock + "}\n```");

    private static string Json(string body) => "```json\n{" + body + "}\n```";

    private static ChatMessage ToolCall(string toolName, Dictionary<string, object?> arguments)
    {
        FunctionCallContent call = new(
            callId: $"demo_{toolName}_{Guid.NewGuid():N}",
            name: toolName,
            arguments: arguments);
        return new ChatMessage(ChatRole.Assistant, [call]);
    }

    private static bool ContainsDate(string s)
    {
        // crude yyyy-MM-dd detector; good enough for demo branching.
        for (int i = 0; i + 9 < s.Length; i++)
        {
            if (char.IsDigit(s[i]) && char.IsDigit(s[i + 1]) && char.IsDigit(s[i + 2]) && char.IsDigit(s[i + 3])
                && s[i + 4] == '-' && char.IsDigit(s[i + 5]) && char.IsDigit(s[i + 6])
                && s[i + 7] == '-' && char.IsDigit(s[i + 8]) && char.IsDigit(s[i + 9]))
                return true;
        }
        return false;
    }

    private static string ExtractMemberQuery(string user) =>
        string.IsNullOrWhiteSpace(user) ? "Jane Smith" : user;
}
