---
name: facility-search-conversational-skill
description: Conversational rubric for resolving the performing facility with POS validation, capabilities, and verification.
version: 1.0.0
tags: [facility-search, conversational-agent, healthcare]
---

# Facility Search Conversational Agent Rubric

{{include:_shared/phi-protection-rules.md}}

{{include:_shared/verification-rules.md}}

{{include:_shared/conversation-rules.md}}

---

## Grid 1 · Search parameters

| Search method | Required | Optional | MCP tool | When to use |
|---|---|---|---|---|
| NPI lookup | facility NPI (7 digits) | plan id | `search_facilities` | Operator has NPI |
| Name + city | name, city | state | `search_facilities` | Operator has name |
| Name only | name | type | `search_facilities` | Operator has just a name |
| Type + capability | type, capability | city | `search_facilities` | Operator knows modality (e.g. "MRI-capable outpatient") |
| POS validation | facility NPI, CPT | – | `validate_pos_for_cpt` | Always after resolution |
| Capability validation | facility NPI, CPT | – | `validate_facility_for_procedure` | Always after resolution for imaging/surgical CPTs |
| Certifications | facility NPI | – | `get_facility_certifications` | Always after resolution |

---

## Grid 2 · Result filtering

| Count | In-network | POS | Capability | Action |
|---|---|---|---|---|
| 0 | – | – | – | broaden |
| 1 | in | valid | capable | verify |
| 1 | in | invalid POS | any | flag, propose anyway (rules engine will re-check) |
| 1 | in | valid | missing capability | flag, suggest alternative |
| 1 | out-of-network | any | any | flag, ask operator |
| Many | mixed | mixed | mixed | table, prefer in-network + valid POS + capable |

---

## Grid 3 · Verification questions

3-of-4 matrix across: **NPI, name, POS, accreditation**.

| Scenario | Ask |
|---|---|
| Resolved | (1) confirm facility name (2) confirm POS code (3) confirm address/city |
| Ambiguous | (1) pick row from table |
| Missing capability | (1) confirm operator wants to proceed anyway |

---

## Grid 4 · Adaptive progression

| Current | Result | Next |
|---|---|---|
| NPI | not found | name+city |
| Name+city | not found | name only |
| Name only | not found | type+capability |
| Type+capability | not found | ask operator to call back |
| Resolved, OON | operator declines | in-network type+capability search |
| Resolved, missing capability | always | propose alternative with capability |

---

## Grid 5 · Data visibility

| Field | Pre-verify | Post-verify |
|---|---|---|
| Name | ✅ | ✅ |
| Type | ✅ | ✅ |
| City / state | ✅ | ✅ |
| NPI | ❌ (mask last 3) | ✅ full |
| POS code | ❌ | ✅ |
| Accreditations | ❌ | ✅ |
| Capabilities | ⚠️ (only what's relevant to CPT) | ✅ full list |
| Network status | ✅ (yes/no) | ✅ full |

---

## Grid 6 · Error handling

| Scenario | Action | Message |
|---|---|---|
| NPI invalid format | ask again | "Facility NPI must be 7 digits." |
| Facility closed | disqualify | "That facility has closed. Searching alternatives." |
| POS mismatch | proceed with flag | "POS [X] is not typical for CPT [Y]; rules engine will re-check." |
| Missing capability | propose alternative | "[Facility] cannot perform [CPT]. Suggested: [alternative]." |

---

## Grid 7 · Command emission

**After explicit operator confirmation of a resolved facility:**

```json
{"action":"set_facility","facilityNpi":"<npi>"}
```

**In every other case:**

```json
{"action":"none"}
```

Read `<npi>` (7-digit) from the "Proposed facility:" bullet list in the
previous assistant turn. CRITICAL: without `set_facility` the workflow will
not advance from the Facility step.
