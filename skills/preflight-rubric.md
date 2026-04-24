---
name: preflight-conversational-skill
description: Rubric for the deterministic pre-flight dry-run. The agent explains outcomes verbatim from the rules engine; it never invents a verdict.
version: 1.0.0
tags: [preflight, conversational-agent, healthcare]
---

# Pre-flight Conversational Agent Rubric

{{include:_shared/phi-protection-rules.md}}

{{include:_shared/verification-rules.md}}

---

## Grid 1 · Parameters

| Required | Optional | MCP tool |
|---|---|---|
| canonical PA request | as-of date, plan id | `preview_criteria_evaluation` |
| canonical PA request | – | `get_policy_text` (for the citation + full text) |

The canonical PA request has: `memberId`, `cpt`, `requestingNpi`, `facilityNpi`,
`icd10`, `conservativeTreatmentWeeks`, `notes`.

---

## Grid 2 · Not applicable

Pre-flight is evaluative, not a search. Filtering happens inside the deterministic
rules engine, whose result the agent reports verbatim.

---

## Grid 3 · Not applicable

The operator has already verified the individual slots in the previous steps.
Pre-flight requires no additional verification.

---

## Grid 4 · Adaptive progression

| Outcome | Next | Message |
|---|---|---|
| `auto-approve` | → Submit | "Pre-flight clean. Ready to submit?" |
| `pend` + fixable gap | stay at Pre-flight, ask operator to adjust | "Gaps: [list]. Update and re-run?" |
| `deny` | → Submit with warning, or back to offending slot | "Would deny — [reason]. Submit anyway, go back, or cancel?" |

---

## Grid 5 · Data visibility

| Field | Visibility |
|---|---|
| Outcome | ✅ specific (`auto-approve` / `pend` / `deny`) |
| Gaps | ✅ full list |
| Rule explanation | ✅ full text |
| Policy citation | ✅ always visible — required for compliance |
| Policy full text | ✅ always visible |
| Internal rules-engine state | ❌ not caller-facing |

The deterministic engine's result is never withheld — clinicians and operators
need to see *why* the verdict came out as it did.

---

## Grid 6 · Error handling

| Scenario | Action | Message |
|---|---|---|
| Rules engine timeout | retry once | "Experiencing a delay. Re-running pre-flight." |
| Missing canonical field | block, back to offending slot | "Missing [field]. Going back to [step]." |
| Policy version mismatch | proceed with newer policy, note version | "Using policy v[X]. Citation: [Y]." |
