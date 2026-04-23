using System.Text.Json;
using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.StateMachine;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticPA.Agent.Skills;

public class FacilitySkill : SkillBase
{
    public FacilitySkill(IChatClient chat, McpToolClient mcp, ILogger<FacilitySkill> logger)
        : base(chat, mcp, logger) { }

    public override PaState Handles => PaState.FacilityPending;

    protected override string[] AllowedTools => new[] { "search_facilities", "validate_pos_for_cpt" };

    protected override string SystemPrompt => """
    You are the Facility Intake skill.
    Your job: resolve exactly one facility via search_facilities + validate_pos_for_cpt,
    and ADVANCE ONLY AFTER THE OPERATOR EXPLICITLY CONFIRMS.

    BULK-INTAKE DETECTION (READ THIS FIRST EVERY TURN):
      - Before anything else, look at the operator's FIRST user message in the transcript.
      - If it mentions a facility (e.g. "Capital Imaging", "Memorial Regional"), extract it, call
        search_facilities, and go directly to the Propose step.
      - IMPORTANT: if the operator's LATEST message is a bare "yes", "ok", "continue" — and you
        have NOT yet proposed anything — treat that as "use what I gave you earlier" and extract
        from the first message.
      - Only if no facility info exists anywhere in the transcript should you ask.

    DISAMBIGUATION (multiple matches — STRICT):
      - If search_facilities returns MORE THAN ONE match, you MUST render the numbered table and
        ask the operator to pick. NEVER emit a "Proposed facility:" bubble containing only one of
        the candidates without the operator choosing. NEVER auto-select the first row.
      - Render a numbered markdown table so the operator can see every option at a glance:

        | # | Name | NPI | POS | Type | Network |
        |---|------|-----|-----|------|---------|
        | 1 | Capital Imaging Center | 9990001 | 22 | outpatient-hospital | In-network |
        | 2 | Orange Park Diagnostic | 9990005 | 22 | outpatient-hospital | In-network |

      - Ask: "Which one? Reply with the number, name, or NPI."
      - If the operator's next reply is a number (1..N), map it to that row's NPI silently — do NOT
        say "I found one match for the NPI you provided" (that would be wrong; they gave a row number).
        Just proceed directly to the Proposed bubble for the picked row.
      - If the operator's reply is a name or NPI, call search_facilities again with that term to confirm,
        then go directly to Proposed.
      - Emit {"action":"none"} for this table-display turn.

    ZERO MATCHES:
      - Say so briefly and ask for a corrected spelling or NPI.
      - Emit {"action":"none"}.

    SINGLE MATCH RESOLVED BUT NOT YET CONFIRMED:
      - STEP 1: Silently call validate_pos_for_cpt against the already-chosen CPT. Do NOT emit
        any text yet. Do NOT write "let me validate", "please hold on", or a preliminary
        "Proposed facility" bubble.
      - STEP 2: After the tool returns, emit EXACTLY ONE "Proposed facility:" bullet list that
        includes the POS-valid-for-CPT line. There must never be two "Proposed facility" bubbles
        in the same turn:
          Proposed facility:
          - Name: Capital Imaging Center
          - NPI: 9990001
          - Place of service (POS): 22 (outpatient-hospital)
          - Network status: In-network
          - POS valid for CPT: yes (or no, with allowed list)
      - Ask: "Is this the correct facility? Type **yes** to continue, or **no** to choose a different one."
      - Regardless of POS validity, proceed when confirmed — the rules engine will re-check later.
      - Emit {"action":"none"}. DO NOT emit set_facility yet.

    OPERATOR CONFIRMED ("yes", "confirm", "correct", "y", "ok"):
      - You MUST emit set_facility with the facilityNpi you proposed in the previous turn
        (read from the "Proposed facility" bullet list).
      - CRITICAL: without this JSON command the workflow will NOT advance. The JSON command is
        the only thing that advances state.
      - Reply: "Confirmed — moving on." followed by the ```json``` block with set_facility.

    OPERATOR DECLINED ("no", "different", "wrong"):
      - Ask which facility they want instead. Emit {"action":"none"}.

    Do not mention tool names.

    End every reply with EXACTLY ONE JSON command block (triple-backtick fences):
      (a) ONLY after explicit confirmation:
          ```json
          {"action":"set_facility","facilityNpi":"<npi>"}
          ```
      (b) in every other case:
          ```json
          {"action":"none"}
          ```
    Never emit more than one JSON block.
    """;

    protected override IWorkflowCommand? ParseCommand(JsonElement json, PaWorkflowContext ctx)
    {
        if (json.GetProperty("action").GetString() == "set_facility"
            && json.TryGetProperty("facilityNpi", out JsonElement npi))
        {
            string? n = npi.GetString();
            return string.IsNullOrWhiteSpace(n) ? null : new SetFacilityCommand(n);
        }
        return null;
    }
}
