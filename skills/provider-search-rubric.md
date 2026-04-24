---
name: provider-search-conversational-skill
description: Conversational rubric for resolving the requesting provider with credentials, network status, and verification.
version: 1.0.0
tags: [provider-search, conversational-agent, healthcare]
---

# Provider Search Conversational Agent Rubric

{{include:_shared/phi-protection-rules.md}}

{{include:_shared/verification-rules.md}}

{{include:_shared/conversation-rules.md}}

---

## Grid 1 · Search parameters

| Search method | Required | Optional | MCP tool | When to use |
|---|---|---|---|---|
| NPI lookup | NPI (10 digits) | plan id | `search_providers` | Operator has NPI |
| Name + state | last name, state | first name, specialty | `search_providers` | Operator has name |
| Name only | last name | specialty | `search_providers` | Operator has just a name |
| Specialty + state | specialty, state | plan id | `search_providers` | Operator knows type + location |
| Network verification | NPI, plan id | – | `verify_provider_network` | Always after resolution |
| Credentials | NPI | – | `get_provider_credentials` | Always after resolution |

### Validation

- NPI: 10 digits.
- State: 2-letter code.

---

## Grid 2 · Result filtering

| Count | Credentials | Network | Action | Message |
|---|---|---|---|---|
| 0 | – | – | broaden search | "Couldn't find that provider — let me search by name only." |
| 1 | active license, no sanctions | in-network | verify | "I found a potential provider. To verify…" |
| 1 | lapsed license OR sanctions | any | flag, disqualify | "That provider has a licensing issue. Let me search alternatives." |
| 1 | active | out-of-network | flag, proceed with operator confirmation | "That provider is out-of-network. Continue? (yes/no)" |
| Many | mixed | mixed | table + filter | Render numbered table; keep only in-network + clean-credentials unless operator overrides. |

---

## Grid 3 · Verification questions

3-of-5 matrix across: **NPI, name, specialty, state, network status**.

| Scenario | Ask |
|---|---|
| Provider resolved | (1) confirm name (2) confirm specialty (3) confirm state |
| Ambiguous | (1) pick row from table |
| OON flagged | (1) explicit ok to proceed OON |
| Sanctions flagged | (1) explicit override OR pick alternative |

---

## Grid 4 · Adaptive progression

| Current | Result | Next |
|---|---|---|
| NPI lookup | not found | name+state |
| Name+state | not found | name only |
| Name only | not found | specialty+state |
| Specialty+state | not found | ask operator to call back |
| Resolved, OON | operator declines | name+state search for alternative |
| Resolved, sanctioned | always | specialty+state alternative |

---

## Grid 5 · Data visibility

| Field | Pre-verify | Post-verify |
|---|---|---|
| Name | ✅ | ✅ |
| Specialty | ✅ | ✅ |
| State | ✅ | ✅ |
| NPI | ❌ (mask last 4 shown) | ✅ full |
| License state / expiry | ❌ | ✅ |
| Board certifications | ❌ | ✅ |
| Sanctions | ⚠️ flag only | ✅ full list |
| Network status | ✅ (yes/no only) | ✅ full (plan, dates) |

---

## Grid 6 · Error handling

| Scenario | Action | Message |
|---|---|---|
| NPI invalid format | ask again | "NPI must be 10 digits. Can you re-provide?" |
| License lapsed | disqualify | "That provider's license has expired. Searching alternatives." |
| CMS preclusion (sanction) | disqualify | "That provider is precluded. Searching alternatives." |
| All methods exhausted | route to manual | "Couldn't locate a qualified provider. Please contact [X]." |

---

## Grid 7 · Command emission

**After explicit operator confirmation of a resolved provider:**

```json
{"action":"set_requesting_provider","npi":"<npi>"}
```

**In every other case:**

```json
{"action":"none"}
```

Read `<npi>` (10-digit) from the "Proposed requesting provider:" bullet list
in the previous assistant turn. CRITICAL: without `set_requesting_provider`
the workflow will not advance from the Requesting Provider step.
