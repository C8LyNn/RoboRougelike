# HAZ-001 — R1 QA evidence (2026-10-21)

Implementation SHA: `ac362ec736f40ab6317102e63e4ea44460aadecd`  
Draft PR: https://github.com/C8LyNn/RoboRougelike/pull/12  
Issue: https://github.com/C8LyNn/RoboRougelike/issues/7

## Observed CI results
- CI-01, run 39683239799: **SUCCESS**, validator regression tests and repository integrity passed.
- CI-02, run 39683239890: **FAIL** at Unity activation credentials; Unity EditMode step **SKIPPED / NOT RUN**.
- CI-03, run 39683239789: **FAIL** at Unity activation credentials; Unity PlayMode step **SKIPPED / NOT RUN**.
- Windows build: **NOT RUN**. Game integration: **NOT INTEGRATED**.

The 8 EditMode and 4 PlayMode tests are **source cases only**; there is no Unity execution evidence.

## Scope and limits
- PR #12: 26 changed paths, all under the dedicated HAZ-001 directory and its .meta or Docs/ParallelMechanics/HAZ-001.
- The pre-existing zone script .meta was preserved; all newly added scripts, asmdefs and directories have their own .meta GUIDs.
- DamageRequested emits requests only; no HP or combat systems are changed.
- NonAlloc scan buffer holds 64 colliders; LastScanTruncated flags possible overflow.
- A visual ring and manual additive demo are provided, but have not yet been inspected in Unity.

## Next gate
1. Import and compile with Unity 6000.3.14f1 in a dedicated checkout.
2. Execute EditMode and PlayMode and preserve XML, logs and exact results.
3. Inspect the additive demo and collision behavior; stress-test 64+ colliders.
4. Coordinate any later gameplay adapter with Codex; do not auto-merge.

GitHub issue-comment and PR-body updates were attempted but blocked by connector safety checks. This in-branch report is the fallback audit trail.
