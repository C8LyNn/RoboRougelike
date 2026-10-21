# RAC-001 — timed checkpoint race / R1 standalone

**Unity:** 6000.3.14f1. **Issue:** #8. **PR:** #13 (DRAFT → develop). **Status:** source staged, Unity tests NOT RUN, gameplay NOT INTEGRATED.

## Functionality
- Time-limited ordered course with immutable checkpoint positions per attempt; exposes Idle/Running/Won/TimedOut/Invalidated.
- One checkpoint per step; reaching another checkpoint out of order does not score.
- Starting inside the first checkpoint, or finishing inside an overlapping next checkpoint, requires exit then fresh re-entry.
- Swept line-segment / sphere test catches fast movement across a checkpoint between frames.
- Excessive displacement is rejected using configurable maximum speed + 0.25m tolerance per step; rejected jumps disarm the checkpoint. This is a gameplay guard, **not authoritative anti-cheat** against a malicious client.
- Timer advances using deltaTime and expires at the exact time limit before checkpoint scoring.
- Pure deterministic CheckpointRaceState, MonoBehaviour CheckpointRaceChallenge with opt-in Transform binding and UnityEvents; reset/restart.
- No input, steering, combat, AI, garage, reward, scene or prefab modifications.

## Demo (opt-in; does not save assets)
Unity Editor: **Tools → RoboRougelike → RAC-001 → Open Unsaved Demo (Additive)**. Creates an unsaved additive scene with 3 markers and a sample cube (not the actual vehicle). Enter Play Mode, click **Start / Restart**. The demo moves only its own cube, and shows progress and timer in OnGUI. **Save nothing** unless intentionally exporting a separate sample scene. Close the additive demo scene without saving when finished.

## Using with a real vehicle (manual, not integrated)
In a separate test scene, attach CheckpointRaceChallenge to a new GameObject, assign the vehicle root Transform to runner and checkpoint Transforms in order; configure radius, limit, maximum speed. Invoke Begin() when starting the challenge; listen to OnCheckpoint/OnWon/OnTimedOut. It reads Transform.position only. Do **not** attach automatically to the existing Gauntlet scene or change existing R1/R3/R4/R6/MOV scripts. An actual runtime bridge requires separate authorization and QA.

## Test plan
- 9 EditMode NUnit cases for initial overlap, ordering, swept crossings, implausible jumps, deadline, invalid configuration, overlapping checkpoints, reset and invalid input.
- 3 PlayMode UnityTest cases for single-fire events, timeout/reset and missing runner.
- Run from an isolated Unity 6000.3.14f1 checkout: `Tools/CI/Run-Unity.ps1 -Task EditMode`, `-Task PlayMode`, `-Task Build`.
- Unity EditMode / PlayMode / Windows build: **NOT RUN** in this environment (no Unity executable/license). Tests are authored, not certified. See QA_R1.md.

## Scope
Only `Assets/RoboRougelikeMechanics/RAC001CheckpointRace/**` and `Docs/ParallelMechanics/RAC-001/**` are modified by R1. The original folder .meta GUID and CheckpointRaceChallenge.cs.meta GUID are preserved. No merge, force push, release, main/develop edits.

