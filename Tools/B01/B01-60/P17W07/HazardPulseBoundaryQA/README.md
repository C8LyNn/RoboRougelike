# B01-60 — PASS17 WAVE07 / independent HAZARDPULSE #430 source QA

**Immutable upstream:** original B01-25 [#430](https://github.com/C8LyNn/RoboRougelike/pull/430) at `6095577ece27320b1a861a738cc23ba42d6793a6`, runtime Git blob `a7a2c69c80d43bf027f2b00896df72f892fa1a17`.

This is a **standalone B01-60 QA receiver** (NOT a producer revision). The new test runner references the **actual unmodified C#9** `PulseCore.cs`, not a Python simulation or a mock. It tests phase transitions, the once-per-activation damage authorization contract, frame partition invariance, numerical boundaries, no double hit, pause, reset, invalid input atomicity, offset safety and player exposure.

## Reproducer / independent target
Original producer tests use a quarter/eighth-second partition and pass. Real gameplay may supply `0.1s` substeps, which are legal (`<=0.25s`). At the mathematically identical **1.0s** activation boundary, ten 0.1s updates can accumulate to 0.9999999999999999 while four 0.25s updates reach exactly 1.0. The test `FRAMERATE_BOUNDARY_0_1S_v_0_25S` asserts equality of `Phase` and `DamagePulse`. A failure is a **real source-level timing regression candidate** and should be reported to the producer without modifying its frozen package. No approval or fix is presumed.

Run from repo root:
```sh
python Tools/B01/B01-60/P17W07/HazardPulseBoundaryQA/source_gate.py
dotnet build Research/B01-25/P17/HAZARDPULSE001/PulseUnityCompat.csproj -c Release -warnaserror
dotnet build Tools/B01/B01-60/P17W07/HazardPulseBoundaryQA/HazardPulseIndependentQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-60/P17W07/HazardPulseBoundaryQA/HazardPulseIndependentQA.csproj
```

**Source G0 gate:** 9 original producer file Git objects + author manifest match, 8 negative mutations, branch direct descendant of source SHA and 6 QA-only paths. GitHub Actions executes real C# on Windows and Ubuntu and preserves RED logs; this QA runner purposely returns nonzero if a property fails.

**Boundary:** No assets under `Assets/**` modified, no real map/profiles, original producer/Codex/CHL/PARTSIZE source mutated, no canonical damage, game VFX or PhysX integration. Native Unity Editor/PlayMode/Player/real HP = **NOT_RUN**. Formal B01-40 receiver and Codex ACK remain independent gates.

**Rollback:** close Draft QA PR / delete only own QA branch and QA paths/workflow. Original #430 remains byte-for-byte immutable. Notify B01-25 of genuine RED, request new owner-approved revision, never mutate #430 producer SHA.
