# CI41-REPLAY-001 v1.1.0 — WAVE 04 / real full-suite Unity reproduction adapter

**Owner:** B01-41 CI/CD & TEST AUTOMATION. **Immutable ancestor:** [CI41-REPLAY-001 v1.0.0 Draft #400](https://github.com/C8LyNn/RoboRougelike/pull/400) @ `0a7c00655f9c84ee50a2bc8ff5f3733440dbbfdf`. **This is a NEW version on a new branch**; #400 is frozen and unchanged.

## Specific defect and repair

v1.0.0 offered a real native-Editor subprocess runner but supported **only `-testFilter`**. The two original Codex [#383](https://github.com/C8LyNn/RoboRougelike/pull/383) failures both passed an isolated re-run (2/2) while full native EditMode failed (2/1330). Consequently repeating only the two filtered tests in v1.0.0 **cannot reproduce suite-order-dependent failures**. WAVE04 extends the *same real subprocess implementation*, without copying or editing Codex gameplay, with an explicitly disjoint `--full-suite --expected-full-cases 1330` mode. It invokes the native Editor **without any `-testFilter`**, requires all 1330 executed `test-case` nodes with matching NUnit root totals and discovery count, and retains the original XML, log, stdout, stderr, exact game SHA, process exit, and SHA-256 of each run. Missing/truncated/renamed/test-count-mismatched runs are HOLD, never PASS.

This does **not** repair the Codex camera/physics gameplay failure; it provides a reproducible native QA procedure to distinguish the two real full EditMode failures from their green isolated retests. It leaves full-suite test order under Unity's original runner; a manifest cannot independently establish cause or physics nondeterminism.

### Exact original game source and original native observation

- **Original gameplay SHA:** `203a0805251ed28293888e43f2d49f0df63767c6`.
- **Original source of native evidence:** `f92c873bb1e397a598b133e107ae704e15f86768` ([Codex PR #383](https://github.com/C8LyNn/RoboRougelike/pull/383)).
- Original **full EditMode: 1330 cases, 1327 PASS, 2 FAIL, 1 SKIP, exit 2**.
- Full EditMode failing tests: `Camera275NativeTests.ScrollZoomIsSmoothAndRemainsAnchoredToVehicle` (1.00205421 m vs <1 m), `MapgenRC1IntegrationTests.MeasuredHeavySpawnsOnGeneratedFloorWithCanonicalEnemies` (grounded wheel count 0 vs >0).
- Conditional policy SKIP: `B0124EnemyAiNegativeEditModeTests.SolidOpaqueCover_SuppressesShot_ThenRecovers_ConditionalPolicy`. **Never forcibly enable without HQ.**
- Original PlayMode **474/474 PASS** and filtered failures 2/2 PASS; no proof the full EditMode is fixed.

## First native action for an authorized Codex/B01-61 receiver

**Prerequisites:** actual Unity **6000.3.14f1** Editor executable, separate/exclusive clean checkout of the exact **gameplay SHA** (NOT PR #400 or this QA branch), sufficient Unity license, no other Unity process using that project's `Library/`. The executable Python `unity_repeat.py` should be available *outside* that checkout (copy the versioned tool to a separate QA scripts directory if needed). `--output` must name a **new** directory outside the checkout, which will not be overwritten.

1. Reproduce the **original full-suite context**. Example PowerShell:

```powershell
python "C:\QA\CI41-v1.1\unity_repeat.py" `
  --project "C:\QA\Codex-Game-203a0805" `
  --editor-exe "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" `
  --source-sha 203a0805251ed28293888e43f2d49f0df63767c6 `
  --mode EditMode --full-suite --expected-full-cases 1330 `
  --repeats 3 --timeout 1800 `
  --output "C:\QA\Results\codex-full-edit-001"
```

2. Compare with isolated tests under the **same game SHA** and another *unique* output directory:

```powershell
python "C:\QA\CI41-v1.1\unity_repeat.py" `
  --project "C:\QA\Codex-Game-203a0805" `
  --editor-exe "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" `
  --source-sha 203a0805251ed28293888e43f2d49f0df63767c6 `
  --mode EditMode `
  --test "Camera275NativeTests.ScrollZoomIsSmoothAndRemainsAnchoredToVehicle" `
  --test "MapgenRC1IntegrationTests.MeasuredHeavySpawnsOnGeneratedFloorWithCanonicalEnemies" `
  --repeats 3 --timeout 900 `
  --output "C:\QA\Results\codex-filtered-edit-001"
```

The full mode deliberately rejects an accidental `--test`; the filtered mode deliberately rejects `--expected-full-cases`. Both fail closed on source SHA mismatch or tracked Unity source dirtiness. Full-mode count pin of 1330 prevents accidentally reporting 2 green filtered tests as full-suite PASS. The native Editor can update its generated `Library/` and other disposable outputs; it should not alter tracked `Assets/Packages/ProjectSettings`. If it does, source guard returns HOLD. Unity-version/environment or package drift may change actual test discovery; **investigate, don't silently relax the 1330 gate**.

**Interpret results:** Each trial's `results.xml` and native `unity-editor.log` remain physically present, with `summary.json` recording `scope=FULL_SUITE`, `expected_full_cases=1330`, per-case results, exit code and XML SHA-256. If some trials pass and others fail, report the split to B01-61/Codex with exact SHA, runner version and logs. If all fail, that is reproducible failure; a Python wrapper failure is not a Codex gameplay PASS. If all pass, the script may return `REPEAT_PASS`, but `full_suite_g1=FULL_SUITE_RECEIPT_REQUIRES_INDEPENDENT_NATIVE_QA`: only actual receiving independent QA can accept native G1/G2. The prior original #383 failed full suite **remains failed historical evidence**.

## Producer tests executed versus tests NOT executed

[Dedicated workflow](https://github.com/C8LyNn/RoboRougelike/actions/workflows/b01-41-p17w03b-unity-replay.yml) verifies both original B01-41 source blobs and runs **18 physical-subprocess tests per OS**. The first 12 verify the original filtered cases; the 6 new tests run the actual OS process and NUnit XML test fixture that simulates **1330 discoverable cases**, including a 1327 PASS / 2 FAIL / 1 SKIP result matching the *shape* of the historical Codex #383 evidence; another is a simulated fully green 1330-run, and negatives reject truncated result coverage, mismatched discovery counts, accidentally mixing filtered and full modes, and test the full CLI with an expected exit 2. **The fake Python Editor is not Unity**; these 36 original source-level checks do not establish native G1. No Codex game code or Unity runtime was executed in these producer Actions.

## Exact scope, receiver, rollback

Only five existing owner files are changed in this version: `Tools/B01/CI41/P17W03B/{unity_repeat.py,test_unity_repeat.py,README.md,manifest.json}` and `.github/workflows/b01-41-p17w03b-unity-replay.yml`. No edits to `Assets/**`, original Codex branch, PARTSIZE/CHL, ruleset, CI02/CI03 or main/develop. All original file paths are in the existing CI41 owner scope.

**B01-40**: inspect v1.1 exact SHA, source Git blobs, test logs, manifest, upgrade compatibility, and rollback. **B01-61 + B01-60**: independently rerun simulated controls, then perform the two native invocations above only in an authorized Unity environment, preserve original per-trial ZIP/logs and exact game SHA, compare full-versus-filtered, and send defects to Codex without modifying his code. **B01-00** chooses whether to queue v1.1 via #79 and requires explicit **Codex ACK** before the state `CODEX_CLAIMED`. An offered Draft PR is not independent QA or ACK.

**Rollback:** leave this new Draft PR unmerged; retain historical frozen v1.0.0 #400 if useful. No game assets, prefabs, GUID, scenes or save data migration. For a native QA repro, discard the separate disposable `Library/` after preserving logs.

**Status:** producer source verification only; B01-40 G0 and B01-60/61 QA pending; native Unity in CI41 producer NOT_RUN, game G1 HOLD, G2 HOLD, Codex ACK pending, not integrated.
