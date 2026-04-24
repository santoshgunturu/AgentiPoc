---
name: submit-conversational-skill
description: Rubric for the final submission step — operator confirms, rules engine runs authoritatively, audit record is written.
version: 1.0.0
tags: [submit, conversational-agent, healthcare]
---

# Submit Conversational Agent Rubric

{{include:_shared/phi-protection-rules.md}}

{{include:_shared/verification-rules.md}}

---

## Grid 1 · Parameters

| Required | Optional | MCP tool |
|---|---|---|
| canonical PA request | – | `submit_pa` |
| submitted canonical + outcome | – | `audit_submission` |

---

## Grid 2 · Not applicable

Submission is terminal. No filtering.

---

## Grid 3 · Operator confirmation

Submit acts as a single confirmation gate: the operator explicitly says "yes / confirm / submit"
to trigger the deterministic engine. The agent must not submit on implicit signals.

---

## Grid 4 · Adaptive progression

| Post-submission outcome | Next | Message |
|---|---|---|
| `auto-approve` | → Done | "PA approved. Case ID [X]." |
| `pend` | → Done with peer-to-peer hook | "PA is pending. Routed to UM Nurse." |
| `deny` | → Done with appeal hook | "PA denied per [policy]. Appeal options available." |

---

## Grid 5 · Data visibility

| Field | Visibility |
|---|---|
| Outcome | ✅ specific |
| Case ID | ✅ always shown after submission |
| Audit trail ID | ✅ always shown |
| Policy citation | ✅ always shown |
| Internal audit record | ✅ (operator can request full export) |

---

## Grid 6 · Error handling

| Scenario | Action | Message |
|---|---|---|
| Audit write failure | retry, then escalate | "Audit write failed — do not close; escalating." |
| Duplicate submission | block | "A case for this request is already open: [ID]." |
| Payer ack timeout | proceed, mark pending | "Submission accepted; payer acknowledgement pending." |
