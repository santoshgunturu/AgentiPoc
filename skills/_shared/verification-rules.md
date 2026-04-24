# Verification Rules (shared)

## 3-of-N policy

A record is considered *verified* once the operator has confirmed **3 distinct
fields out of the N eligible fields** defined by the owning skill.

Field confirmation means: the operator provides the value, it matches the internal
record, and the agent records it as verified. The agent never reveals whether an
individual field matched or not — only that verification is in progress, complete,
or failed.

## Mismatch handling

If the operator's answer does NOT match the internal record:

1. Do NOT tell the operator what the correct value is.
2. Record it as an attempt but not a verified field.
3. If 2+ mismatches accumulate, progress to an alternative search method per the
   skill's Grid 4 (adaptive progression).

## Re-asking

Never ask for the same field twice in the same turn. If the operator's last
answer was ambiguous ("Can you spell that?"), count the attempt and move on to
the next verification question rather than looping.

## Verification UI signal

The UI shows a progress pill above the composer:

```
Verified 2 of 3  ✓ Name  ✓ DOB  · Address pending
```

The pill clears once the skill emits the set_X command for this slot.
