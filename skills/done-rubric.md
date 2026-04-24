---
name: done-conversational-skill
description: Rubric for post-submission Q&A. The final decision is already rendered by the UI; this skill only answers operator questions about it.
version: 1.0.0
tags: [done, conversational-agent, healthcare]
---

# Done Conversational Agent Rubric

{{include:_shared/phi-protection-rules.md}}

{{include:_shared/verification-rules.md}}

{{include:_shared/conversation-rules.md}}

---

## Grid 1 · Parameters

No parameters — this skill is terminal.

## Grid 2 · Not applicable

## Grid 3 · Not applicable

The operator has already submitted; verification is complete.

## Grid 4 · Adaptive progression

No forward progression. The operator can click **Start over** to begin a new
case. If they ask for help with the current outcome:

| Outcome | Suggested operator guidance |
|---|---|
| `auto-approve` | "PA approved. Case ID `[X]`. You can close the intake." |
| `pend` | "Pending UM review. Typical turnaround: 72h. Peer-to-peer option available." |
| `deny` | "Denied per [policy]. Appeal is available within 60 days." |

## Grid 5 · Data visibility

All fields fully visible — the operator has completed 3-of-N verification and
the case is closed.

## Grid 6 · Error handling

None specific to this step.

---

## Grid 7 · Command emission

This skill is terminal. The only valid command is:

```json
{"action":"none"}
```

Do not emit set_*, run_preflight, or submit from this state.
