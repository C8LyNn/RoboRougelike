# B01 RUN-SAFETY-001 / R1 isolated proof of concept
HQ P0 dispatch `HQ-B01-20261025-S04` · [Issue #152](https://github.com/C8LyNn/RoboRougelike/issues/152) · ECON #136.

The gameplay issue: once destroyed parts must wait for one real **won** challenge, a dead gun/drive/support chain makes `CanDeploy=false` and `StartNextEncounter` refuses the win needed for the rebuild.
This package provides pure C# eligibility, a bounded single-challenge **loaner lease**, duplicate-proof victory event and separately simulated one-free-rebuild-per-winning-cycle ECON queue. It does **not** change the real run state, destroy/revive parts in R3, grant rewards, spawn production vehicles or modify defeat.
Source `Assets/B01Packages/RUNSAFETY001/Runtime/RunSafetyState.cs`; standalone visual `Demo/RUNSAFETY001_Demo.unity` uses only primitive physics; NUnit Edit+Play source, asmdefs and Unity GUID .meta. Runtime has no R1/R3/R6 assembly dependencies.
Contract review: `POLICY_OPTIONS.md`, `CURRENT_API_AUDIT.md`; implementation prompt and rollback `INSTALL_FOR_CODEX.md`, `PROMPT_FOR_CODEX.md`.
Native Unity tests have not been executed by producer; B01-60/40 QA and HQ final policy approval are required before any handoff. Exact SHA pinned in issue/PR after source freeze; NO MERGE.
