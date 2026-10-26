# B01-20 / W14 CAMERA R1.2.3 — one-frame retarget evidence integrity

## Confirmed source logic defect (not a confirmed RC2 camera/gameplay fault)
In frozen [CAMERA R1.2.2 Draft #262](https://github.com/C8LyNn/RoboRougelike/pull/262) SHA `063e755c50c9728f0a9627d25d3677280625a73d`, `Camera001AimTimeline.Capture` only rejected a later `FrameIndex <= previous.FrameIndex`. A recorder could capture frame100 then frame110, compare a target angular change accumulated over ten missing frames, but compute allowed turret slew using only the **current** `DeltaTimeSeconds` (for example 1/60s). This can falsely mark `TARGET_STEP_EXCEEDS_TURRET_BUDGET_PLAUSIBLE` and misroute CAM-R12-INT-01 to the gun owner.

## Minimal runtime FIX / actually authored tests
In **package-owned passive** `Camera001AimTimeline.cs` only:
- Reject negative native frame IDs.
- Reject any skipped frame using checked-width subtraction `(long)frame.FrameIndex - previous.FrameIndex != 1L`, preserving previous duplicate/reversal `NONMONOTONIC_FRAME` guard and fail-closed `SKIPPED_FRAME` reason.
- All checks run **before** state mutation and `Camera001AimTransitionDiagnostics.Measure`.
- Valid contiguous records and existing global camera/weapon behavior are unchanged; a native comparator must **start a new recorder per continuous phase** if frames are missing, not silently backfill.
- Existing original CAMERA Edit5, R1.2.1 Edit6, R1.2.2 Edit6, Play13+2 unchanged; add **2 new** named Unity EditMode negative tests `SkippedFramesCannotBeMisclassifiedAsSingleFrameGunSlew`, `NegativeFrameRejectedAndMaxIntFrameCannotWrap`. Expected new authored total **19 EditMode and 15 PlayMode**, NOT_RUN on Unity by producer.
- Exact runtime diagnostic+timeline C# under explicit Unity math surrogate .NET8 now runs 5 additional negative cases: dropped-frame rejection, transactional state, first next contiguous frame success, negative index and wrap guard; prior 1280 tests retained. New count subject to final actual CI logs.
- Static negative mutation in `check_w13_contract.py` verifies the guard is present before `Measure` and deliberately removing it is detectable.

## What this DOES NOT do
Does not fix or retune the **canonical** 170deg/s R3/P12 gun, target-ray acquisition, camera final pose, user controls, physics, Unity rendering, RC2 production files or the original native Light720/1080 15.306°/13.104° tracking failure. Native CAMERA R1.2 [QA#249](https://github.com/C8LyNn/RoboRougelike/pull/249) remains prior external evidence: compile Edit5/5 Play13/13 PASS and camera pose observations PASS, but full Windows tracking 2/8 FAIL; cause not proved camera.

## Single actionable licensed QA request (B01-00 to Codex/B01-60)
In **a disposable Unity6000.3.14f1** worktree, import complete inherited source #134+#214+#247+#251+#262+this W14 Draft at exact pinned producer SHA, preserving .meta and target `Assets/B01Packages.meta`. Compile raw Editor.log/native exit, run full **19 Edit +15 Play** raw NUnit XML; confirm two new W14 guards are GREEN. Then perform **only** the still-open matched A/B/C Light720/1080 W+LMB5s native source comparisons (BASE P12 / CAMERA R1.1 / CAMERA R1.2), with same real R3 loadout/physics/input, recording each *consecutive rendered frame* actual target collider ID/type and aim point, canonical muzzle world/local direction, chassis angular delta, actual turret slew and camera final pose. Treat dropped samples as **invalid source evidence**, never a positive gun-rate failure. Preserve original #249 FAIL logs and screenshots; do not weaken ≤0.05° thresholds; report exact source/QA/target SHA and raw CSV/XML/Player exits.
If absent licensed Windows runner, mark all new native stages **NOT_RUN**; do not claim G1. B01-40 independent source/GUID/G0 target proof remains needed; G2 HOLD, no merge, no live Codex game edits.

## Install and rollback (G2 HOLD)
Receiver verifies full deterministic inner ZIP against manifest and SHA256_REPORT, installs only into disposable QA worktree; R1.2.3 is strictly stacked on immutable R1.2.2 parent `063e755c50c9728f0a9627d25d3677280625a73d`. On failure revert this W14 overlay only to parent SHA; preserving target's shared B01Packages.meta and parent CAMERA physical GUID; no changes to active RC2, BUILD, R3/R6, Pack, P12, scenes or ProjectSettings.
