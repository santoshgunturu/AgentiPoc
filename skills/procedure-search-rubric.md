---
name: procedure-search-conversational-skill
description: Conversational rubric for resolving a CPT procedure with contraindications, coverage, and verification.
version: 1.0.0
tags: [procedure-search, conversational-agent, healthcare]
---

# Procedure Search Conversational Agent Rubric

{{include:_shared/phi-protection-rules.md}}

{{include:_shared/verification-rules.md}}

{{include:_shared/conversation-rules.md}}

---

## Grid 1 · Search parameters

| Search method | Required | Optional | MCP tool | When to use |
|---|---|---|---|---|
| Exact CPT | CPT code | plan id | `search_procedure_codes` | Operator has CPT code |
| Description fuzzy | description text | body part | `search_procedure_codes` | Operator has words ("MRI knee") |
| Body-part browse | body part | modality | `search_procedure_codes` | Operator knows only body part |
| Rules lookup | CPT | – | `get_procedure_rules` | After resolution to show contraindications |
| Coverage check | CPT, plan id | – | `check_procedure_coverage` | When plan is known |
| Auth requirement | CPT | – | `check_auth_required` | Always — final gate |

### Validation

- CPT codes: 5 digits (or alphanumeric CPT II/III).
- For MRIs, the contrast modifier is required to pick between `73721 / 73722 / 73723`
  and similar triples. Do NOT ask the contrast question if the operator already stated it
  ("without contrast", "with contrast", "with and without").

---

## Grid 2 · Result filtering

| Count | Auth status | Action | Message |
|---|---|---|---|
| 0 | – | broaden | "Couldn't find that procedure — let me search by description." |
| 1 | any | verify | "I found a CPT that matches. Let's verify…" |
| Many | all no-auth | show list, let operator pick | "Found several non-auth procedures. Which one?" |
| Many | all auth-required | show table (CPT, desc, body part) | "Found multiple matches. Pick #, CPT, or description." |

### Contraindication flags

Before proposing a CPT, call `get_procedure_rules` and flag if the context suggests
a contraindication (e.g. pregnancy noted in the intake for an MRI). Flags do not
block — they are disclosed.

---

## Grid 3 · Verification questions

| Scenario | Ask | Required |
|---|---|---|
| CPT resolved | (1) confirm body part / laterality (2) confirm contrast (3) confirm description | 3 of 4 (CPT, body part, contrast, description) |
| Ambiguous | (1) pick row from table | N/A (selection) |
| Contraindication flagged | (1) acknowledge contraindication | 1 extra check |

---

## Grid 4 · Adaptive progression

| Current | Result | Next | Reason |
|---|---|---|---|
| Exact CPT | not found | Description fuzzy | operator typed wrong code |
| Description fuzzy | not found | Body-part browse | operator gave narrow wording |
| Body-part browse | not found | ask operator to call back | no more search options |
| CPT resolved | not covered | ask if operator wants alternatives (from `get_procedure_rules`) | offer paths |

---

## Grid 5 · Data visibility

| Field | Pre-verify | Post-verify |
|---|---|---|
| CPT code | ✅ | ✅ |
| Description | ✅ | ✅ |
| Body part | ✅ | ✅ |
| Auth required | ✅ (vague "likely requires PA" ok) | ✅ (specific yes/no) |
| Contraindications | ✅ | ✅ |
| Prerequisites | ✅ | ✅ |
| Policy text | ❌ (reference only) | ✅ (full text per citation) |

---

## Grid 6 · Error handling

| Scenario | Action | Message |
|---|---|---|
| CPT retired | Suggest replacement if available | "That CPT was retired on [date]. The current code is [X]." |
| Plan mismatch | Flag and continue | "That CPT is not covered under [plan]. Alternatives: [list]." |
| Tool timeout | Retry once then fallback | "Experiencing a delay. Trying again…" |

---

## Grid 7 · Command emission

**After explicit operator confirmation of a resolved CPT (even no-auth CPTs):**

```json
{"action":"set_procedure","cpt":"<cpt>"}
```

**In every other case (asking contrast, disambiguating, operator declined):**

```json
{"action":"none"}
```

Read `<cpt>` from the "Proposed procedure:" bullet list in the previous
assistant turn. CRITICAL: without `set_procedure` the workflow will not
advance from the Procedure step.
