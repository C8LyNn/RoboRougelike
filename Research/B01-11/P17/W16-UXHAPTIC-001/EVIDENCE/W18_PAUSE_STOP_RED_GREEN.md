# B01-11 UXHAPTIC-001 R3 — P17 / W18 original C# pause-stop integration contract

## Prior actual evidence
Immutable W17 R2 at `fa6defe701818e79fec447b95fc94efc1cc76cd1` now has **confirmed executed final cross-OS original C# [Actions #41101261126](https://github.com/C8LyNn/RoboRougelike/actions/runs/41101261126): 126 PASS/0 FAIL Ubuntu and 126 PASS/0 FAIL Windows**, 252 original C# checks. W17 RED [#41100770919](https://github.com/C8LyNn/RoboRougelike/actions/runs/41100770919) 116 PASS/10 FAIL each OS. These are historical R2 results only.

## W18 source issue
The prior `HapticGate` exposes `SetEnabled(false)` to stop a current impulse, but setting disabled is a **user preference**, not a per-run pause state. Without an independent paused state, a valid event during gameplay pause can request a fresh motor impulse or the host has to incorrectly alter the persistent preference to stop vibration. R3 adds `SetPaused` to stop active impulses without muting the preference; paused current-run source events remain validated and sequence-consumed, but create no motor output.

## Test-first evidence and implementation
- Tests written **before production change** at `604443a9c42557633d41519d50c13a5815553d68`, **18 named original source tests** using reflection to compile against older R2 lacking `SetPaused` without test compilation break. [RED original CI #41104581405](https://github.com/C8LyNn/RoboRougelike/actions/runs/41104581405). Mark result NOT_RUN until actual logs are returned; do not invent RED count.
- R3 production source `3875a32a9c1fac5feba98d06ecede99196de926f`: new `bool Paused`, `bool SetPaused(bool)`, `HapticOutcome.SuppressedPaused`, consumes current-run sequence during pause, preserves `Enabled` and last played time, stops active pulse via a returned host-mandatory flag, clears pause on `StartNewRun`.
- [GREEN original source #41104661493](https://github.com/C8LyNn/RoboRougelike/actions/runs/41104661493), expected 144 cases per OS. Actual status must be verified on exact final package SHA before claiming R3 source-green; GitHub queue is not native execution.

## G0/G1/rollback boundaries
Only source-level future command decisions were written. No physical motors, Unity Input System hooks, `Assets/**`, scenes, native tests, Codex code, frozen #510/#556/#596. R3 no Codex OFFER or ACK, no integrator feedback. Independent B01-40 G0 and B01-60 source QA pending, Unity G1 NOT_RUN, no merge. Roll back to immutable R2 `fa6defe701818e79fec447b95fc94efc1cc76cd1` or remove R3 isolated Research.
