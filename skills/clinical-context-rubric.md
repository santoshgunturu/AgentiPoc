---
name: clinical-context-conversational-skill
description: Conversational rubric for capturing ICD-10, conservative treatment, and clinical notes with procedure pairing validation.
version: 1.0.0
tags: [clinical-context, conversational-agent, healthcare]
---

# Clinical Context Conversational Agent Rubric

{{include:_shared/phi-protection-rules.md}}

{{include:_shared/verification-rules.md}}

---

## Grid 1 · Search parameters

| Search method | Required | Optional | MCP tool | When to use |
|---|---|---|---|---|
| Exact ICD-10 | icd10 code | – | `search_diagnosis_codes` | Operator has the code |
| Description fuzzy | description | category | `search_icd10_hierarchy` | Operator has words |
| Hierarchy browse | category | chapter | `search_icd10_hierarchy` | Operator knows clinical area |
| Pairing validation | icd10, cpt | – | `validate_icd_procedure_pairing` | Always before confirming |

### Fields to capture

1. **ICD-10** — the diagnosis supporting the requested procedure
2. **Conservative-treatment weeks** — integer; required for most policy rules
3. **Notes** — brief free text (ice, NSAIDs, bracing, PT program, etc.)

### Validation

- ICD-10 format: uppercase letter + digits + optional dot (e.g. `M17.12`, `S83.512A`)
- PT weeks: non-negative integer

---

## Grid 2 · Result filtering

| Count | Pairing | Action |
|---|---|---|
| 0 | – | fuzzy search fallback |
| 1 | valid | propose |
| 1 | invalid | flag, ask if operator has a supporting dx |
| Many | mixed | table with categories; prefer codes whose `category` aligns to CPT |

---

## Grid 3 · Verification questions

3-of-4 matrix across: **ICD-10, description, PT weeks, notes**.

| Scenario | Ask |
|---|---|
| All fields captured | (1) confirm ICD+description (2) confirm PT weeks (3) confirm notes |
| Missing field | ask for the missing one |
| Pairing invalid | (1) confirm dx (2) offer to broaden to related codes |

---

## Grid 4 · Adaptive progression

| Current | Result | Next |
|---|---|---|
| Exact ICD | not found | description fuzzy |
| Description fuzzy | not found | hierarchy browse |
| Hierarchy | not found | ask operator to describe symptoms |
| Pairing invalid | always | hierarchy browse by CPT's expected category |

---

## Grid 5 · Data visibility

| Field | Pre-verify | Post-verify |
|---|---|---|
| ICD-10 code | ✅ | ✅ |
| Description | ✅ | ✅ |
| Chapter / category | ✅ | ✅ |
| PT weeks | ✅ | ✅ |
| Notes | ⚠️ (echo back redacted) | ✅ full |

Clinical notes are always audit-logged even if not shown in full to the operator.

---

## Grid 6 · Error handling

| Scenario | Action | Message |
|---|---|---|
| Invalid ICD format | ask again | "ICD-10 format: letter + digits + optional dot." |
| ICD-CPT mismatch | flag, suggest | "That diagnosis doesn't typically support [CPT]. Alternatives: [related codes]." |
| Insufficient PT | flag, continue | "Policy requires [N] weeks; you provided [M]. Rules engine will re-check." |
