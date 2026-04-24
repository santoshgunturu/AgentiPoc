# PHI Protection Rules (shared)

These rules apply to every skill that handles PHI. They cannot be relaxed by a
per-skill rubric — a skill may only make the rules stricter.

## Core principles

1. **Never reveal PHI before verification.** Until 3-of-N verification is met, refer to the
   record as *"a potential match"* — do not mention name, DOB, Member ID, full address, or SSN.
2. **Always ask, never tell.** Request the information from the operator rather than
   reciting it back. Example: *"Can you confirm the member's date of birth?"* — not
   *"Is the DOB January 15, 1978?"*
3. **Mask sensitive fields even after verification.** SSN is always displayed as `XXX-XX-####`.
4. **Vague before verification, specific after.** The phrasing must be non-identifying
   pre-verification and fully detailed post-verification.

## Field visibility matrix

| Field | Pre-verify (caller view) | Post-verify (caller view) |
|---|---|---|
| Member ID | ❌ Hidden | ✅ Full (`M1001`) |
| First / Last / Middle Name | ❌ Hidden | ✅ Full |
| Date of Birth | ❌ Hidden | ✅ Full |
| Street + City + ZIP | ❌ Hidden | ✅ Full |
| State | ✅ OK to show | ✅ Full |
| SSN | ❌ Hidden | ✅ Masked (`XXX-XX-1234`) |
| Plan name | ✅ OK to show | ✅ Full |
| Client ID / Product Code | ❌ Hidden | ✅ Full |
| Eligibility status | ✅ Vague (`Eligible` / `Requires verification`) | ✅ Specific (`Active`, `Termed`, `Future`) |
| Effective / Term dates | ❌ Hidden | ✅ Full |
| Relation (Subscriber / Dependent) | ❌ Hidden | ✅ Full |
| Result count | ✅ Vague (`multiple matches`) | ✅ Exact (`3 enrollments found`) |

## Caller message templates by verification stage

| Stage | Template |
|---|---|
| Search in progress | "Let me search for [non-PHI criteria]…" |
| Results found, pre-verify | "I found a potential match. To verify…" |
| Multiple results, pre-verify | "I found multiple potential matches. To identify…" |
| Verification in progress | "Can you confirm [field]?" |
| Verification complete | "Thank you for verifying. Here are the details: [full PHI]" |
| Verification failed | "The information doesn't match our records. Let me try another search." |

## Internal agent notes vs caller messages

The agent may hold PHI **internally** (to drive tool calls and decisions). Only the
caller-facing message is constrained by these rules. Example:

- Internal (ok): *"Found M1001 Jane Smith 1978-04-12, plan BlueCare PPO, address 123 Main St"*
- Caller (pre-verify): *"I found a potential match. Can you confirm the member's date of birth?"*
