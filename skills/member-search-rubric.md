---
name: member-search-conversational-skill
description: Conversational agent rubric for RBM 7 Member Search — guides operators through adaptive search while protecting PHI.
version: 1.1.0
tags: [member-search, conversational-agent, healthcare, phi-protection]
---

# Member Search Conversational Agent Rubric

This file defines the decision rules, search parameters, filtering logic, and
data visibility rules for the conversational member-search workflow.

**Before making any decision, always read this file to understand the current policy.**

{{include:_shared/phi-protection-rules.md}}

{{include:_shared/verification-rules.md}}

{{include:_shared/conversation-rules.md}}

---

## Grid 1 · Search parameter requirements

| Search method | Section | Required | Optional | MCP tool | When to use | Minimum criteria |
|---|---|---|---|---|---|---|
| Member ID – All Clients | A | Member ID | Date of Service | `search_members` | Caller has Member ID, no health plan specified | Member ID present |
| Name/DOB – All Clients | B | First Name (2+ chars), Last Name (2+ chars), DOB | Date of Service, Member Number | `search_members` | Caller has name and DOB, no health plan | First ≥2 AND Last ≥2 AND DOB |
| Member ID – By Health Plan | C | Member ID, Health Plan OR State | DOS | `search_client_specific_members` | Caller has Member ID and knows plan | ID AND (Plan OR State) |
| Name/DOB – By Health Plan | D | First, Last, DOB, Plan OR State | DOS, Member #, SSN | `search_client_specific_members` | Caller has name, DOB, and knows plan | First ≥2 AND Last ≥2 AND DOB AND (Plan OR State) |
| Name – By Health Plan | E | First, Last, Plan OR State | DOB, DOS, Member #, SSN | `search_client_specific_members` | DOB not available but plan is | First ≥2 AND Last ≥2 AND (Plan OR State) |
| Anthem BC Enrollments | Special | Member ID, Client ID | DOS, Contract # | `search_anthem_bc_enrollments` | Anthem BC specifically | ID AND clientID = Anthem BC |

### Parameter validation

| Parameter | Rule | Action if invalid |
|---|---|---|
| First Name | length ≥ 2 | ask for valid |
| Last Name | length ≥ 2 | ask for valid |
| DOB | MM/DD/YYYY or yyyy-MM-dd | ask for valid format |
| Member ID | not empty | ask for valid |
| Health Plan | must match catalog | show list, ask to pick |

Always uppercase first and last name before sending to MCP.

---

## Grid 2 · Result filtering rules

| Result count | Eligibility | Filtering | Next action | Caller message | Progress? |
|---|---|---|---|---|---|
| 0 | – | none | auto-progress | "Let me try another search method…" | ✅ |
| 1 | Eligible | none | verify | "I found a potential match. To verify…" | ❌ verify |
| 1 | Ineligible | discard | auto-progress | "Let me try a broader search…" | ✅ |
| 1 | Termed | discard | auto-progress | "Let me search for current enrollment…" | ✅ |
| Many | All eligible | keep all | narrow | "Multiple potential matches. To identify…" | ❌ narrow |
| Many | All ineligible | discard | auto-progress | "Let me try another method…" | ✅ |
| Many | Mixed (some eligible) | keep eligible | verify or narrow | "Let me filter for eligible members…" | depends on count |

### Eligibility treatments

| Status | Include? |
|---|---|
| Eligible / Active | ✅ |
| Future | ⚠️ only if no active found |
| Historical | ⚠️ only if no active/future |
| Termed / Ineligible | ❌ |

### Multiple-enrollment selection

| Scenario | Selection logic |
|---|---|
| DOS provided | pick enrollment where Effective ≤ DOS ≤ Term |
| No DOS, multiple active | latest Effective |
| No active | latest Effective regardless of status |
| Same name/DOB twins | ask for middle name / address / ID |

---

## Grid 3 · Verification questions by scenario

| Scenario | Agent knows internally | Caller provided | Questions to ask | Verification count |
|---|---|---|---|---|
| Member ID success | ID, Name, DOB, Address | ID | (1) confirm name (2) confirm DOB (3) confirm address | need 2 more (have ID) |
| Name/DOB success | Name, DOB, ID, Address | Name + DOB | (1) confirm ID (2) confirm address | need 1 more (have Name+DOB) |
| Same-name/DOB | Multiple records | First, Last, DOB | (1) middle name (2) member ID (3) address | depends |
| Multiple enrollments | One member, many periods | identifying info | (1) date of service (2) which period | selection, not verification |
| Verification failed | search result doesn't match | partial info | (1) spell first name (2) exact DOB | restart verification |
| Need health plan | name/DOB only | Name, DOB | (1) plan name (2) state of issuance | gathering criteria |
| Termed/Ineligible found | record exists, not eligible | identifying info | (1) currently enrolled? (2) DOS? | routing decision |

### 3-of-4 matrix

| ID | Name | DOB | Address | Valid? |
|---|---|---|---|---|
| ✅ | ✅ | ✅ | ❌ | ✅ |
| ✅ | ✅ | ❌ | ✅ | ✅ |
| ✅ | ❌ | ✅ | ✅ | ✅ |
| ❌ | ✅ | ✅ | ✅ | ✅ |
| any 2 of 4 | | | | ❌ |

### Question templates

| Field | Template | Expected | Validation |
|---|---|---|---|
| Member ID | "Can you confirm the member ID number?" | alphanum | exact (case-insensitive) |
| Full Name | "Can you confirm the member's full name?" | First [Middle] Last | exact First+Last |
| First Name | "What is the member's first name?" | text | exact (CI) |
| Last Name | "What is the member's last name?" | text | exact (CI) |
| Middle Name | "What is the member's middle name or initial?" | text/char | exact (CI) |
| DOB | "What is the member's date of birth?" | date | exact |
| Address | "Can you confirm the member's address?" | Street, City, State ZIP | street+city; ZIP optional |
| Health Plan | "What is the member's health plan?" | name | match to catalog |

---

## Grid 4 · Adaptive progression rules

| Current | Result | Eligibility | Next | Caller message | Skip if |
|---|---|---|---|---|---|
| A: ID-All | 0 | – | → B | "Let me search by name and DOB." | – |
| A: ID-All | Only ineligible | all ineligible | → B | "Let me try a broader search…" | – |
| A: ID-All | 1+ eligible | any | Verify | "I found a potential match. To verify…" | – |
| B: Name/DOB-All | 0 | – | → C | "Let me search within a specific health plan." | – |
| B: Name/DOB-All | Only ineligible | all ineligible | → C | "Let me try health-plan-specific." | – |
| B: Name/DOB-All | 1+ eligible | any | Verify | "I found a potential match." | – |
| C: ID-HP | 0 | – | → D | "Let me search by name within the plan." | no Member ID → skip to D |
| C: ID-HP | Only ineligible | – | → D | "Let me try name-based." | – |
| D: Name/DOB-HP | 0 | – | → E | "I've searched all available methods." | – |
| D: Name/DOB-HP | 1+ eligible | any | Verify | "I found a potential match." | – |
| E: Unable to Locate | Member found, termed/ineligible | – | → F | "I found a record but it needs eligibility review." | – |
| E: Unable to Locate | No member | – | → G | "Couldn't locate this member in our system." | – |

### Section skip logic

| Scenario | Skip | To | Reason |
|---|---|---|---|
| No Member ID | A | B | cannot perform ID search |
| No plan/state known | C | E | cannot perform plan-specific search |
| No ID AND no plan | C | D | skip ID search, go straight to name/DOB by plan |
| All methods done | D | E | exhausted |

### Decision tree

```
START
  ↓
Has Member ID?
├── YES → A → eligible? → YES: VERIFY / NO: → B
└── NO  → B → eligible? → YES: VERIFY / NO:
                            Has plan info?
                            ├── YES → C/D → eligible? → YES: VERIFY / NO: → E
                            └── NO  → E → member found?
                                          ├── YES → F (override/eligibility)
                                          └── NO  → G (manual add)
```

---

## Grid 5 · Data visibility rules

Governed by `_shared/phi-protection-rules.md`. Member-specific overrides: none.

---

## Grid 6 · Error handling

| Scenario | Detection | Action | Caller message | Recovery |
|---|---|---|---|---|
| MCP timeout | > 30s | retry once | "Experiencing a delay. Retrying…" | alt method |
| MCP error | tool error response | log, notify | "Trouble with that search. Trying a different approach." | alt method |
| Invalid date | parse fail | ask again | "Please provide DOB in MM/DD/YYYY." | re-request |
| Plan not found | no catalog match | show list | "Couldn't find that plan. Available: [list]." | let caller pick |
| Verification mismatch | answer ≠ internal | do not reveal | "Let me try another method." | progress |
| All methods exhausted | A–D exhausted | route → E | "Searched all methods. Checking options." | E → F or G |
| Conflicting info | new contradicts old | clarify | "Earlier you said X, now Y. Can you clarify?" | re-confirm |
| Can't narrow | still multi after all Qs | ask for unique id | "Still multiple. Can you provide the address?" | request unique |
| Caller lacks info | missing required | suggest alt | "Without [X], we need to route to the plan directly." | RBM 14 |
| Member, no enrollments | empty list | suggest alt DOS | "Member found, no enrollments on file. Different DOS?" | alt DOS or manual add |

---

## Grid 7 · Command emission

This skill advances the state machine by emitting ONE of the JSON commands
below, following the shared conversation rules:

**After explicit operator confirmation of a single resolved member:**

```json
{"action":"set_member","memberId":"<id>"}
```

**In every other case (disambiguating, zero matches, asking for DOB, user
declined the proposal, etc.):**

```json
{"action":"none"}
```

Read `<id>` from the "Proposed member:" bullet list in the most recent
assistant turn. CRITICAL: without `set_member` the workflow will not advance
from the Member step, regardless of what the prose says.

---

## Decision policy summary

**Core principles:** protect PHI · adapt automatically · verify thoroughly
(3-of-4) · guide conversationally · exhaust all options.

**Success path:** Gather → Search → Filter → Verify (3 of 4) → Display full
details → SUCCESS.

**Failure paths:**
1. Search → no results → try next method → exhaust → route to RBM 8/14.
2. Search → results → verification failed → try next → exhaust → RBM 14.
3. Search → termed/ineligible only → try next → exhaust → RBM 8/14.
