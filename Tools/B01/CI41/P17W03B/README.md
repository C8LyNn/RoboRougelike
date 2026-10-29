# CI41-REPLAY-001 v1.0.0 — repeatable real Unity Editor filtered regression executor

**Producer:** B01-41 — CI/CD & TEST AUTOMATION. New functional package; no `Assets/**`, canonical Codex code, CHL/PARTSIZE or shared CI02/03 edits. Python 3 standard library + Git. No standalone Unity license included.

## What this package actually does

The [`unity_repeat.py`](./unity_repeat.py) script is a **functional Unity Editor CLI executor**, not another static XML analyzer. When an authorized receiver supplies a real Unity 6000.3.14f1 Editor executable and an **exclusive clean worktree at the requested exact game commit**, the script actually launches the Editor N times via `subprocess.run`, running requested NUnit test names. Each trial gets its own preserved `results.xml`, `unity-editor.log`, `stdout.txt`, `stderr.txt`, and a summary record containing source SHA, process exit, timeout, exact filtered cases, result counts and XML SHA-256. An existing evidence directory is never overwritten. Every run verifies exact original `HEAD` and clean tracked `Assets/Packages/ProjectSettings` source before and after Editor invocation.

**Result semantics:** `REPEAT_PASS` (exit 0) only if every requested, actually executed filtered test passed, all native process exits are zero and expected cases exactly match XML; `HOLD_REPRO_OR_INVALID_EVIDENCE` or `FAIL_CLOSED` (exit 2) otherwise. No XML, unexpected test names, contradictory counts, skipped tests, source changes, timeouts, or nonzero Editor process returns cannot be green. The tool never grants **full-suite G1**: filtered pass only means those tests passed in those attempts.

This is **distinct from [CI41-NATIVE-TRIAGE-001 #388](https://github.com/C8LyNn/RoboRougelike/pull/388)**, which *reads historical native XML*. This package **executes and repeats the command itself** when a real Unity binary is supplied, retaining each attempt so Codex can measure intermittent failures. Also distinct from GUID [#345](https://github.com/C8LyNn/RoboRougelike/pull/345), dependency impact [#352](https://github.com/C8LyNn/RoboRougelike/pull/352) and path-portability [#378](https://github.com/C8LyNn/RoboRougelike/pull/378).

## Intended first Codex use, after authorized native QA intake

The two full-suite failures from Codex's original #383 receipt were:

- `Camera275NativeTests.ScrollZoomIsSmoothAndRemainsAnchoredToVehicle`
- `MapgenRC1IntegrationTests.MeasuredHeavySpawnsOnGeneratedFloorWithCanonicalEnemies`

On a **separate checkout of Codex gameplay SHA** `203a0805251ed28293888e43f2d49f0df63767c6`, not a concurrently used Editor project, run in PowerShell (substitute the actual Unity Editor path, and choose a fresh output directory):

```powershell
python Tools/B01/CI41/P17W03B/unity_repeat.py `
  --project "C:\\Work\\RoboRougelike-P17-ExactSHA" `
  --editor-exe "C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.14f1\\Editor\\Unity.exe" `
  --source-sha 203a0805251ed28293888e43f2d49f0df63767c6 `
  --test "Camera275NativeTests.ScrollZoomIsSmoothAndRemainsAnchoredToVehicle" `
  --test "MapgenRC1IntegrationTests.MeasuredHeavySpawnsOnGeneratedFloorWithCanonicalEnemies" `
  --mode EditMode --repeats 10 --timeout 600 `
  --output "C:\\Work\\CI41-Evidence\\replay-001"
```

**Important:** This should be run from a location where the script is available; the actual `--project` points to the exact Codex-game source worktree, not the B01-41 Draft branch. You may copy the optional standalone Python script into the authorized QA environment. **Never use an Editor concurrently against the same `Library/` worktree**. If the source changed mid-run the script returns HOLD; existing XML remains for investigation. Unity `-testFilter` is a semicolon-separated list of exact names; this tool checks the returned test cases to prevent passing an unintended filter. The caller must ensure that the selected filters are recognized by the Unity test runner and should check its own logs for runner version-specific argument behavior. The script also supports one test at a time.

**Why this matters:** both historically failing tests passed 2/2 in isolated native re-runs even though full EditMode originally failed. Repeating the isolated pair can reveal intermittent behavior but cannot prove or rule out order-dependent regressions. To resolve the full-suite block, a fresh full EditMode run and independent B01-60/61 QA are still required.

## Truly executed dual-OS producer tests

[`test_unity_repeat.py`](./test_unity_repeat.py) runs **12 actual subprocess fixture tests per OS** using a short Python executable acting as a *simulated* Unity CLI. These are physical processes that write real NUnit-format XML and logs; not just text matching or a mock of subprocess.run. Tests cover three actual repeated invocations with the two original Codex test names, failure exit and red XML, zero exit + red XML, nonzero exit + green XML, missing/malformed XML, inconsistent counters, duplicate/unrelated test cases, genuine process timeout, incorrect source SHA, tracked dirty assets, unsafe filters, output overwrite prevention, and a real complete CLI invocation.

[Original Actions](https://github.com/C8LyNn/RoboRougelike/actions/runs/40911232379) exercises physical simulated processes on Windows 2025 and Ubuntu 24.04; original log artifacts are provided there. **NATIVE UNITY = NOT_RUN IN CI41 PRODUCER ACTIONS.** Earlier initial run [#40911049812](https://github.com/C8LyNn/RoboRougelike/actions/runs/40911049812) had 12/12 unittest passes per OS but workflow failure from a mismatched expected *grep marker* for timeout fixture; only the workflow assertion was corrected, no changes to implementation/tests or game.

## Import, independent QA, rollback and blockers

- Receiver **B01-40** checks own five-file manifest/sha/source scope/optional tool behavior and rollback.
- **B01-60/61** independently reruns simulated subprocess tests, then optionally authorizes real native Editor execution on exact Codex game source. The producer GitHub Actions runner does **not** have the game/Unity license, so no native QA is asserted.
- **B01-00** schedules possible integration in CODEX inbox #79; Codex explicitly acknowledges owner transfer. A producer comment or Draft PR is not `CODEX_ACK`.
- **Rollback:** leave own Draft PR unmerged or remove optional script/workflow from a later authorized CI adaptation. The tool does not write game `Assets/` or `ProjectSettings/` source; native Unity may update the local generated `Library/` directory when run. Preserve/revert that disposable QA checkout if necessary.

**Known limitations:** filtered single-/double-case runs are not the complete full-suite, Unity native execution is environment-dependent and unrun in this source producer, tests use intentionally constrained exact filter syntax (no unsafe arbitrary command injection), stdout/stderr are capped to 8 MB each, timeout killing a parent Editor is not a guarantee of terminating any unrelated child process, and an external native Editor can change untracked build artifacts. No guarantee of cross-session hardware/timing determinism or root-cause attribution.

**State:** SOURCE_TOOL_EXECUTED_BY_SIMULATOR / NATIVE_UNITY_NOT_RUN_BY_PRODUCER / G0_B01_40_PENDING / INDEPENDENT_QA_B01_60_PENDING / CODEX_ACK_PENDING / G1_HOLD / G2_HOLD / NOT_INTEGRATED / NO_MERGE.
