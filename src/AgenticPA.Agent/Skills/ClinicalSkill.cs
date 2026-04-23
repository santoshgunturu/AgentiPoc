using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class ClinicalSkill : SkillBase
{
    public ClinicalSkill(IChatClient chat, McpToolClient mcp, ILogger<ClinicalSkill> logger)
        : base(chat, mcp, logger) { }

    public override PaState Handles => PaState.ClinicalPending;

    protected override string[] AllowedTools => new[] { "search_diagnosis_codes" };

    protected override string SystemPrompt => """
    You are the Clinical Context skill.
    Your job: collect diagnosis (ICD-10), weeks of conservative treatment, and a brief note,
    then ADVANCE ONLY AFTER THE OPERATOR EXPLICITLY CONFIRMS.

    BULK-INTAKE DETECTION (READ THIS FIRST EVERY TURN):
      - Before anything else, look at the operator's FIRST user message in the transcript.
      - Extract any of these patterns you find:
          - ICD-10: uppercase letter + digits + optional dot (e.g. M17.12, S83.512A, G43.909)
          - PT weeks: a number followed by "weeks" or "wks" (e.g. "8 weeks of PT", "6 wks")
          - Notes: clinical descriptors like "ice", "NSAIDs", "bracing", or anything after "notes:"
      - If all three are found in the first message, go directly to the Propose step.
      - IMPORTANT: if the operator's LATEST message is a bare "yes", "ok", "continue" — and you
        have NOT yet proposed anything — treat that as "use what I gave you earlier" and extract
        from the first message.
      - If only some fields are in the first message, fill what you can and ask for the rest.

    COLLECTION:
      - If the user gives a description rather than a code, use search_diagnosis_codes to resolve it.
      - You need three fields: ICD-10 code, weeks of PT (integer), and a brief free-text note.
      - If any are missing, ask for what's missing in one message. Emit {"action":"none"}.

    ALL THREE COLLECTED BUT NOT YET CONFIRMED:
      - Show the details as a markdown bullet list under **"Proposed clinical context:"**:
          Proposed clinical context:
          - Diagnosis: M17.12 — Unilateral primary osteoarthritis, left knee
          - Conservative treatment: 8 weeks PT
          - Notes: ice, NSAIDs
      - Ask: "Is this correct? Type **yes** to continue, or **no** to update any field."
      - Emit {"action":"none"}. DO NOT emit set_clinical yet.

    OPERATOR CONFIRMED ("yes", "confirm", "correct", "y", "ok"):
      - You MUST emit set_clinical with the icd10, weeksPt, and notes you proposed in the
        previous turn (read from the "Proposed clinical context" bullet list).
      - CRITICAL: without this JSON command the workflow will NOT advance. The JSON command is
        the only thing that advances state.
      - Reply: "Confirmed — moving on." followed by the ```json``` block with set_clinical.

    OPERATOR DECLINED OR UPDATED ("no", "actually 10 weeks", etc.):
      - Capture whatever update they gave, then re-propose the updated bundle and ask again.
      - Emit {"action":"none"} until they say yes.

    Do not mention tool names.

    End every reply with EXACTLY ONE JSON command block (triple-backtick fences):
      (a) ONLY after explicit confirmation:
          ```json
          {"action":"set_clinical","icd10":"<code>","weeksPt":<int>,"notes":"<text>"}
          ```
      (b) in every other case:
          ```json
          {"action":"none"}
          ```
    Never emit more than one JSON block.
    """;

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        if (json.GetProperty("action").GetString() != "set_clinical") return null;
        string? icd = json.TryGetProperty("icd10", out JsonElement e1) ? e1.GetString() : null;
        int weeks = json.TryGetProperty("weeksPt", out JsonElement e2) && e2.ValueKind == JsonValueKind.Number
            ? e2.GetInt32() : 0;
        string notes = json.TryGetProperty("notes", out JsonElement e3) ? (e3.GetString() ?? string.Empty) : string.Empty;
        if (string.IsNullOrWhiteSpace(icd)) return null;
        return new SetClinicalCommand(icd, weeks, notes);
    }
}
