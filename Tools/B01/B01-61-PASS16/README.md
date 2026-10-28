# B01-61 PASS16 — original Codex regression receiver (QA-only)

**Owner** B01-61. **Canonical game owner** Codex. Current source-game pin [`aa7a917f177a9b1f07203868fb7d1d871eb3ee46`](https://github.com/C8LyNn/RoboRougelike/commit/aa7a917f177a9b1f07203868fb7d1d871eb3ee46), later provenance/evidence pin [`589b60a912096d1e9917cb0bbac73a53e7543b4f`](https://github.com/C8LyNn/RoboRougelike/commit/589b60a912096d1e9917cb0bbac73a53e7543b4f). Original `#315` garage→map fixture has been re-pinned to nine current source/test Git blobs without being installed in Codex. This is an independently executable *QA tool and an authored native test fixture*, **not new game code or G1**.

## Delivery A — actual executed original Codex Unity receipt audit

- [`manifest.json`](manifest.json) pins every original XML/process/test source Git blob and declares exact expected 1303 Edit / 474 Play / 15 retest. The old 15-FAIL original EditMode was **actually executed by Codex**, not B01-61; no `1303/1303` claim. The later targeted 15/15 cannot replace a full post-test-change Unity run. B01-61's own Unity remains **NOT_RUN**.
- [`audit_codex_full_receipts.py`](audit_codex_full_receipts.py) executes on **raw original Git bytes**, refuses invalid XML, spoofed root summary, zero/duplicate fullnames, zero/hidden skipped cases, wrong exit/source/platform, changed raw blobs, and unverified test-name changes. It compares *exactly the original 15 failures* to the 15 targeted re-executions, with **one explicit original-C# verified rename** `RRLW2GameplayTests.RealDestroyedEnemyGunIsStoredRebuiltForFullPriceAndMounted` → `...AtDiscountAndMounted`; costs 36→13 are verified in the actual original old/new test source. No generalized “same count is enough” bypass.
- `python3 Tools/B01/B01-61-PASS16/audit_codex_full_receipts.py --selftest` runs a positive and six adversarial validator negatives. `python3 Tools/B01/B01-61-PASS16/audit_codex_full_receipts.py --repo "$PWD"` requires the **original game and evidence pinned Git commits** locally fetched. `--require-release` returns expected **exit 1 / HOLD**, not release green; any parser/evidence error exits **2**. GitHub Actions runs all these commands on its own read-only runner.

## Delivery B — authored focused, original-source Unity receiver fixture

[`B0161Pass16InputCameraRegressionTests.cs`](B0161Pass16InputCameraRegressionTests.cs) has two new *unrun* `[UnityTest]` cases:
1. **HeldPrimaryThroughPauseIsBlockedUntilActualReleaseAndRepress** — actual `P12PipelineSafeTankController.Update`, virtual Input System LPM pressed, actual `R1RunController.TogglePause()` propagates `SetGameplayInputEnabled(false)`; **held-through-resume must not fire**, release then repress must fire again. This checks existing game guard, not a synthetic combat reward.
2. **MovingVehicleRepositionsActiveCameraWithoutReplacingTargetRoot** — real loaded scene, test-range vehicle translation, actual camera facade target identity and motion, receiver rig not independently stepping, finite zoom. This is a *basic moving-target regression*, **not a camera-jitter/framerate/visual acceptance or independent G1**.

[`verify_pass16_fixture.py`](verify_pass16_fixture.py) is a Python original-*source and fixture*-pin checker and negative mutation detector, **not** C# compilation. Run in the same GitHub Actions workflow after the original game SHA is fetched. The authored fixture's SHA and referenced original controller/camera/run SHA are stored in `manifest.json`.

### Conditional Codex operator / existing CV-04 + CX-07/CX-09

Use **only a Codex-authorized disposable Unity 6000.3.14f1 Windows workspace** with a legitimate activated license, not the Codex active worktree or user's desktop. After Codex verifies game SHA and a compatible existing **EditMode** test assembly, install only the two QA C# files (previous `B0161CrossFeatureRegressionTests.cs` and new `B0161Pass16InputCameraRegressionTests.cs`) into that *disposable copy* and compile; do not overwrite Canonical Assets or existing test files. Run genuine Unity Test Runner `EditMode` with filter `B0161Pass16InputCameraRegressionTests`, plus `B0161CrossFeatureRegressionTests`, bounded deadline and separate result files. Suggested command **only for an already available permitted operator, NOT executed by B01-61**:

```powershell
& "<LICENSED_UNITY_6000.3.14f1_EDITOR>" -batchmode -nographics -projectPath "<DISPOSABLE_PROJECT>" -runTests -testPlatform EditMode -testFilter B0161Pass16InputCameraRegressionTests -testResults "<EVIDENCE_DIR>\B0161-P16-edit.xml" -logFile "<EVIDENCE_DIR>\B0161-P16-Editor.log" -quit
```

**PASS** only if both actual named tests are present and pass with zero skip/failure, valid original XML, no compiler errors, real original 6000.3.14f1 version/license, exact game/test SHAs, exit 0. **FAIL** on missing test assembly/input-device support, source drift, held fire before release, stale/unresponsive camera, or any real test failure; classify **COMPILE_FAIL** separately from **GAMEPLAY_FAIL**. **NOT_RUN** if no permitted licensed environment. Preserve raw XML/Editor.log/process exit and changed source blob receipts. Execute other full game Edit/Play and actual Windows Player only under existing Codex CV-07/CV-08, not merely these two cases.

**Scope:** no CHL/PART edits, no canonical `Assets/**`, no merges, no GUI/human assertions. The original recent Codex full Edit 1288/1303 failed; targeted 15/15 passed after test changes, full updated suite still NOT_RUN as of the pinned evidence. **Release G1/G2 HOLD**.
