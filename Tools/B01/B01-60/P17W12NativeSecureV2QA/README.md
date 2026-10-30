# B01-60 — PASS 17 / WAVE 12 — native Unity/PhysX independent receiver for ONE selected #510

## Scope and immutable source

[HQ #518](https://github.com/C8LyNn/RoboRougelike/issues/518) selects **only** [B01-23 Secure V2 R2 Draft #510](https://github.com/C8LyNn/RoboRougelike/pull/510) on exact frozen original **`d94400d95d13db7f27e1533df05c9292d5380431`**. Frozen V2 parent [#488](https://github.com/C8LyNn/RoboRougelike/pull/488) `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3` contains the old transaction RED demonstrated by prior B01-60 [#507](https://github.com/C8LyNn/RoboRougelike/pull/507); actual source repair #510 uses `AtomicDockLedger` to rearm the real W06 ID *before* Rigidbody/parent changes and compensates on exception. Independent B01-61 source [#517](https://github.com/C8LyNn/RoboRougelike/pull/517) has executed C#9 source regressions. **This new test package does not duplicate them:** it adds **real-native-only** EditMode + PlayMode tests, source-locked disposable Unity project staging, and a strict native XML+Editor.log test runner. Alternative #511, #494 and old W07/08/09 are **not co-installed**. B01-60 never modifies upstream owner/Codex/main/develop.

## Source-only validation: executed on any OS, not a native PASS

From a checkout of this independent Draft branch (stacked exactly on frozen #510), with Python3.11+Git installed:

```sh
python Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py --mode preflight
```

This independently reads every original B01-23 V2 R2 and W06 original Git **blob bytes** from exact pinned SHA, validates original Unity6 `Packages/manifest.json` Test Framework 1.6.0 and `ProjectSettings/ProjectVersion.txt` editor 6000.3.14f1, GUIDs and exact producer/QA-only file scope. It **materializes** two original B01-60 .cs test assemblies (EditMode and PlayMode) with actual Unity `MonoImporter`, `AssemblyDefinitionImporter`, folder and stable generated Unity .meta GUIDs. Creates a reproducible, native Unity6-importable, isolated **complete source/test/project ZIP**, containing only actual #510 + original W06 `Assets/**`, QA tests, original Packages/ProjectVersion; excludes all legacy W07, #511, #494 and Codex code. ZIP audit verifies original per-file SHA-256, actual Git blob pins, 10 adversarial negative tests for modified files, missing file, traversal, legacy/alternative injection, forged native/ACK, disposable staging and strict rollback preserving foreign files. Source preflight PASS means ZIP/metadata ready, **not an executed Unity Editor**.

## Actual native Unity host execution — licensed host only

With the exact repo checkout and an **already licensed** Unity 6000.3.14f1 executable available, run:

```sh
python Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py --mode native --unity "/absolute/path/to/Unity" --out "/absolute/path/to/new/output-evidence-directory"
```

The runner refuses an existing output directory, revalidates original selected Git source, constructs a disposable isolated complete project and invokes the genuine Unity Editor twice using `-batchmode -nographics -quit -projectPath ... -runTests -testPlatform EditMode|PlayMode -testResults ... -logFile ...`. It preserves **two real NUnit XML files, two original Editor.log files, return codes and stdout, physical ZIP and provenance**. A native PASS is allowed **only if** both processes exit 0 and the actual XML parses as Passed, 0 failed, at least **2 independent EditMode + 14 independent PlayMode test cases**. Failing activation, wrong/missing binary, missing XML or actual physics RED results in **NOT_RUN / FAIL / HOLD** as applicable, never a fabricated success. No game files or existing Unity workspaces are changed. Unity may need package registry connectivity and proper license through the authorized host; no activation credentials are stored in this PR.

### Independent native PlayMode acceptance matrix

Our QA tests execute actual **Rigidbody / BoxCollider / Physics.SyncTransforms / Transform** values on **both 1.5kg and 12kg original real owner rigs**. For EACH of three externally callable `Reattach / TrySafeReattach / ReattachFromGui` methods (6 native cases), a solid blocker on Unity physics layer 31 must deny docking **without ANY** change to body, world/local position, world rotation, angular/linear velocity, kinematic flag, Transform parent, `Detached` or consumed-ID count. The next six physical native cases independently clear the consumed ID from the private original R2 `AtomicDockLedger` while detached and test all 2×3 public callers; each must fail with `TransactionRejected` and every real Rigidbody/Transform/ledger state unchanged. One additional genuine Unity Rigidbody test injects a mid-transaction exception through the original `AtomicDockLedger.TryReattach` and verifies **actual 3D pose, parent, velocity, kinematic and original W06 ID restoration** on rollback. One more test injects an exception during rollback and must surface **quarantine** instead of silently succeeding. Two native Unity **EditMode** tests check real assembly import/link of selected controller + W06 and missing-ID fail-closed on original ledger without callbacks. Source tests do **not** establish these real PhysX/Editor observations.

## G0/G1/G2 and rollback

- `QA_SOURCE_PREFLIGHT`: Windows+Ubuntu source Git/provenance+ZIP+meta/rollback test only, not Unity.
- `NATIVE_G1`: **NOT_RUN** when Unity Editor absent or activation unavailable. No XML/Editor.log means no physical game PASS.
- `G2`: **HOLD** until native authorized tests and full migration/legacy exclusion.
- `CODEX_OFFER/ACK/INTEGRATED`: B01-00/#79 ownership only; no fake CLAIMED/OFFERED/ACK.
- `NO_MERGE` and NO producer/old W07/#511/#494/canonical game changes.

**Rollback:** Close this Draft and delete its six own-only paths. Generated Unity project is placed in a disposable temp directory; the stage/rollback check refuses overwrite or removal of altered/foreign files. The host retains only explicitly requested evidence directory with ZIP/receipts and (only upon real native execution) Unity XML/logs. **Receiver:** B01-40 for separate exact-SHA physical owner G0 and migration; B01-61/62 for source regressions; B01-60 native host; B01-00 eventual Codex handoff.
