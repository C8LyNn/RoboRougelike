# B01-61 / W62-W65 PASS12: isolated QA fixtures

**Exclusive QA owner:** B01-61 (CODEX REGRESSION QA). **Codex game source: unchanged.** Dispatch [#314](https://github.com/C8LyNn/RoboRougelike/issues/314). Original fixture was authored in PASS11; PASS12 makes it a durable isolated source-backed artifact with a real automatic CI *static* gate.

Pinned source: [Codex draft #310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`; exact 8 original source/test Git blobs in `manifest.json`. Immutable QA fixture Git blob `9a1c06f1b9e3d5f0a13daecdb19041538d02075e`.

## Two tests to run in licensed, separate Unity

- `[Test] DeepCopyPreservesABAndSuspensionButDoesNotShareMutableState`: clone A/B weapon groups, MOV009 Assist/height, gun identity and scrap; negatively mutate disposable clone and assert original model unchanged.
- `[UnityTest] EditorGarageMapTestReturnPreservesGroupsSuspensionIdsAndScrap`: actual menu/editor → garage with weapon assigned B → install MOV009 Assist → map test → change test-only suspension Full/height → pause and restore editor/garage; check original GUIDs, Scrap, map JSON and A/B assignment. Existing Codex `MapEditorIntegrationTests`, `WeaponGroupTests`, `SuspensionUxTests` test these features separately, not the combined transition.

## Verification and gate limits

`python3 Tools/B01/B01-61-PASS12/verify.py --fixture-only` checks the exact authored Git blob and required test invariants, executable without Unity. Full static gate `python3 Tools/B01/B01-61-PASS12/verify.py --repo "$PWD"` requires having fetched the exact Codex SHA and independently validates eight original Git C# blobs plus five production source-state anchors. The workflow `.github/workflows/b01-61-pass12-static.yml` explicitly fetches and pins Codex PR #310 and fails closed if its head changes. This **is not a C# compiler and not Unity**.

To run the two tests, HQ must authorize a separate licensed Windows Unity **6000.3.14f1** disposable workspace (NOT the Codex active worktree). Copy only `B0161CrossFeatureRegressionTests.cs` into the compatible original existing EditMode assembly, run Unity Test Runner or `-runTests -testPlatform EditMode -testFilter B0161CrossFeatureRegressionTests`, retain original NUnit XML, Editor.log, exit status, Unity version and full source SHA. B01-61 does not claim native execution or PASS.

This fixture intentionally does not re-report B01-61's prior weapon-detach/remount policy risk (R07) or B01-60 D04 camera-loss finding. It is **not** the CHL/PART G1 gate, G2 permission, CODEX_ACK, or merge approval. No canonical gameplay files are changed.
