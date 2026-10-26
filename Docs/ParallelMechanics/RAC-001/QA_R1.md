# RAC-001 — R1 handoff / QA evidence

**Branch:** feature/rrl-rac001-isolated-pack → develop (DRAFT PR #13)
**Base develop at inspection:** 5f06af81e2dd9d877e4300259bb3dd008cb9c9d1
**Before R1:** 689c8f4cb4fd342df48eb49616793a9185ac557b
**Unity version:** 6000.3.14f1

## Delivered
- Deterministic checkpoint race state machine, ordered entry validation, swept crossings, movement sanity guard, timeout, reset.
- Opt-in Unity MonoBehaviour event adapter, independent additive unsaved demo.
- 9 EditMode and 3 PlayMode **authored** test cases, separate test asmdefs.
- All new Unity scripts, assemblies and directories have .meta files; existing GUIDs retained.

## Verification
- Static scope and manifest validation: checked by publishing script (only owned paths, unique new GUIDs).
- Unity EditMode: **NOT RUN** — no Unity 6000.3.14f1 executable/licensed runner available in this execution environment.
- Unity PlayMode: **NOT RUN** — same reason.
- Unity Windows build: **NOT RUN** — same reason.
- Game scene integration: **NOT INTEGRATED** (deliberate, no edit to scene 1).
- CI GitHub Actions: inspect new commit checks separately; do not treat structural PASS as Unity PASS.

## Risks / next
1. Run actual EditMode/PlayMode tests and a Windows build on isolated, licensed Unity 6000.3.14f1; record XML, log, exit code, SHA.
2. Manually inspect the additive demo in Play Mode; verify cube traversal and timer.
3. Only after QA, consider a separately reviewed opt-in real-vehicle adapter without editing Codex-owned files.
4. Keep DRAFT PR open; no merge to develop/main without release QA.


## GitHub Actions for R1 commit `49878cf29c62a5fb9b4714594f1ef98424ad2a49`

- **CI-01 Repository Validation: PASS** — workflow run 39685544651, validator regression and Git index integrity both success.
- **CI-02 Unity EditMode: NOT RUN** — workflow run 39685544695 failed at "Require valid Unity activation credentials"; Unity EditMode step skipped.
- **CI-03 Unity PlayMode: NOT RUN** — workflow run 39685544643 failed at "Require valid Unity activation credentials"; Unity PlayMode step skipped.
- **Windows build: NOT RUN** in this task.
- **Issue #8 / PR #13 update attempt: BLOCKED** by GitHub connector safety checks; this file is the durable handoff until comment/edit permissions permit updates.

Links: https://github.com/C8LyNn/RoboRougelike/actions/runs/39685544651 · https://github.com/C8LyNn/RoboRougelike/actions/runs/39685544695 · https://github.com/C8LyNn/RoboRougelike/actions/runs/39685544643
