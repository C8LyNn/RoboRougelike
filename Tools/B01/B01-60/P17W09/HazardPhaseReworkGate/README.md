# B01-60 — PASS17 / WAVE09 — HAZARDPULSE immutable producer rework acceptance gate

## Purpose and blocker

[HQ #466](https://github.com/C8LyNn/RoboRougelike/issues/466) mandates a **fresh B01-25 owner source revision** to address the genuine fractional timestep defect found by independent [B01-60 QA #444](https://github.com/C8LyNn/RoboRougelike/pull/444). Frozen original producer [#430](https://github.com/C8LyNn/RoboRougelike/pull/430) has exact commit `6095577ece27320b1a861a738cc23ba42d6793a6` and actual original C# blob `a7a2c69c80d43bf027f2b00896df72f892fa1a17`. **A new owner revision was NOT available when this gate was authored.** Do not claim a candidate GREEN merely because expected old RED was reproduced.

This is distinct from author tests and from W07: it automatically retests the **exact unmodified original 10 W07 tests** (frozen program blob `e65ca6134e0730c1a91a7f98cc1da286f6ff08f6`) and a **new 384 whole-second phase-boundary matrix**: 8 actual legal timestep partition patterns × offset 0/3 seconds × 24 seconds, plus 16 total per-cycle one-shot grant count checks. The active loop executes **real source C#9 `PulseZoneController.Advance`** compiled in a separate .NET8 test-only project. This is not a Python gameplay reimplementation.

### Two mutually exclusive modes

- **Baseline (default; GREEN CI only means EXPECTED OLD RED confirmed):** retargets original frozen #430 bytes from Git into an *ephemeral* QA-owned C# file, runs original frozen W07 tests and the new matrix. Requires original **9 PASS / 1 FAIL** with only `FRAMERATE_BOUNDARY_0_1S_v_0_25S` failing; W09 matrix must detect the known 10×0.1s = Warning versus 4×0.25s = Active disagreement. CI succeeds **as a diagnostic** while **original source remains QA RED**. Baseline SUCCESS is NOT package acceptance.
- **Candidate (only after owner publishes a new SHA):** manually trigger the workflow with `source_sha` set to a literal 40-character **B01-25-produced** new commit. The script rejects the original SHA, rejects any out-of-scope or unrelated non-descendant commit, copies only its actual `PulseCore.cs` Git object into the isolated runner, demands **10/10 original independent W07 tests PASS**, plus **zero phase disagreements in the new W09 384-boundary matrix and valid pulse counts**. Outputs exact SHA, blob and raw logs. Until a candidate source commit exists, `CANDIDATE_SOURCE_G0=NOT_RUN`.

Run baseline manually from QA branch checkout (Python3.11, Git, .NET8):

```sh
python Tools/B01/B01-60/P17W09/HazardPhaseReworkGate/retarget.py
```

Run on an immutable new owner SHA after fetching that Git commit, without changing this frozen QA branch (read-only original source):

```sh
CANDIDATE_SOURCE_SHA=<40-character-author-commit> python Tools/B01/B01-60/P17W09/HazardPhaseReworkGate/retarget.py
```

Evidence and nonzero exit codes from both C# runs are archived into `qa-w09-hazard-evidence/` by each CI job. Note source gating is strict and will refuse non-descendant new owner source; coordinate a separately authorized matching receiver revision if the owner chooses a different ancestry.

## Source ownership, engine truth and release gates

All seven files under this PR are **B01-60 own QA and unique workflow**, stacked on exact original producer commit; no edits to `Research/B01-25/**`, `Assets/**`, #444, Codex, CHL, PARTSIZE, main/develop. Generated C# source/test files are test-only, erased after the runner and never committed. The original `PulseCore.cs` executes physically from the selected immutable Git object in dotnet C#9. Original code's `PhaseAt`, `DamagePulse` contract, input timing and cycle behavior remains owner-controlled. A proposed solution or code change belongs ONLY to B01-25; our job is independent RED/GREEN.

**Native Unity 6000.3.14f1 GameObject/PhysX/actual HP/damage/Player/visual telegraph G1=NOT_RUN**, HQ G2 HOLD, Codex ACK PENDING, INTEGRATED NO. Receiving order: B01-25 publishes new version/SHA → B01-60 runs candidate red/green same SHA → B01-40 formal original physical G0 intake → B01-00 alone may offer one selected package through #79 → Codex native integration only after explicit ACK. Rollback: remove this QA-only path and own workflow, do not mutate frozen source.
