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

## W09 ACTUAL independent R1 RED → R2 GREEN — dual-OS exact source evidence

**A real new B01-25 source version was published during W09:** [producer Draft #470](https://github.com/C8LyNn/RoboRougelike/pull/470) frozen R2 SHA `ebc3d6162f4516147092960291e50e15fe6c4582`, original new file `Research/B01-25/P17/HAZARDPULSE001R2/PulseCore.cs` Git blob `42385a88a43a2d5854a00d74ac82e65075b8c56b`. Versioned original R1 files and the W07 immutable QA runner are unchanged. W09 gate was updated only within B01-60 test code to read this NEW R2 path instead of assuming the producer had rewritten the frozen R1 path. It validates the 11-file B01-25 R2 scope and exact ancestry; no producer files were patched by the receiver.

[**Actual GitHub Actions #41054076970**](https://github.com/C8LyNn/RoboRougelike/actions/runs/41054076970) on independent QA test-code SHA `a002f63519fd35d3c68105b78636137ee496e4e9` executed **FOUR COMPLETED SUCCESS jobs**, independently running 2 source revisions × 2 OS (Ubuntu 24.04 and Windows 2025). Windows and Ubuntu original raw logs inspected separately:

| Test | Frozen R1 @ 6095577e | Owner-corrected R2 @ ebc3d616 |
| --- | --- | --- |
| Real, unchanged independent #444 W07 C#9 tests | **9 PASS / 1 FAIL**, reproduced exact named fractional timestep defect, actual runner exit=1 EXPECTED RED | **10 PASS / 0 FAIL**, actual runner exit=0 |
| W09 independently authored phase checks | **38 mismatches / 384** endpoint checks | **0 mismatches / 384** endpoint checks |
| W09 activation authorization cycle checks | 16 executed | 16 executed |
| Compiled original immutable B01-25 C#9 | PASS | PASS |
| Native licensed Unity/PhysX/actual HP and damage | NOT_RUN | NOT_RUN |

Original evidence (negative control): [Windows baseline](https://github.com/C8LyNn/RoboRougelike/actions/runs/41054076970/artifacts/12976321764), [Ubuntu baseline](https://github.com/C8LyNn/RoboRougelike/actions/runs/41054076970/artifacts/12976241790). Corrected R2 acceptance evidence: [Windows R2](https://github.com/C8LyNn/RoboRougelike/actions/runs/41054076970/artifacts/12975143963), [Ubuntu R2](https://github.com/C8LyNn/RoboRougelike/actions/runs/41054076970/artifacts/12974824710).

**VERDICT** `B01_60_INDEPENDENT_HAZARDPULSE_R2_SOURCE_G0_PASS` against exactly owner #470 SHA above, **and frozen #430 R1 still G0 RED**. This is a meaningful independent source-level RED→GREEN on the owner's unchanged new code, not proof of a native Unity prefab/player/HP integration. B01-40 must physically accept new version R2 packaging/GUID/assembly and B01-00 alone may select a future offer to #79; Codex ACK, integration and native G1 are still pending. No merge.
