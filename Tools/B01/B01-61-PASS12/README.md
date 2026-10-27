# B01-61 / PASS12–PASS13: isolated QA fixtures and original NUnit release-evidence audit

**Exclusive QA owner:** B01-61 (CODEX REGRESSION QA). **Codex game source: unchanged.** Dispatch [#314](https://github.com/C8LyNn/RoboRougelike/issues/314). Original fixture was authored in PASS11; PASS12 makes it a durable isolated source-backed artifact with a real automatic CI *static* gate.

Pinned source: [Codex draft #310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`; exact 8 original source/test Git blobs in `manifest.json`. Current immutable QA fixture Git blob `bcbe8353c7e2a78db0565694ced6afb4397e9987` (PASS13 changed QA test only).

## Two tests to run in licensed, separate Unity

- `[Test] DeepCopyPreservesABAndSuspensionButDoesNotShareMutableState`: clone A/B weapon groups, MOV009 Assist/height, gun identity and scrap; negatively mutate disposable clone and assert original model unchanged.
- `[UnityTest] EditorGarageMapTestReturnPreservesGroupsSuspensionIdsAndScrap`: actual menu/editor → garage with weapon assigned B → install MOV009 Assist → map test → change test-only suspension Full/height, swap weapon B→A and set disposable gun HP to zero → pause and restore editor/garage; check original GUIDs, Scrap, map JSON and A/B assignment. Existing Codex `MapEditorIntegrationTests`, `WeaponGroupTests`, `SuspensionUxTests` test these features separately, not the combined transition.

## Verification and gate limits

`python3 Tools/B01/B01-61-PASS12/verify.py --fixture-only` checks the exact authored Git blob and required test invariants, executable without Unity. Full static gate `python3 Tools/B01/B01-61-PASS12/verify.py --repo "$PWD"` requires having fetched the exact Codex SHA and independently validates eight original Git C# blobs plus five production source-state anchors. The workflow `.github/workflows/b01-61-pass12-static.yml` explicitly fetches and pins Codex PR #310 and fails closed if its head changes. PASS13 extends this to 33 SHA/source/fixture static checks and a read-only audit of actual original NUnit XML. [Latest PASS13 original-code CI](https://github.com/C8LyNn/RoboRougelike/actions/runs/40593756705) SUCCESS, not native Unity. This **is not a C# compiler and not Unity**.

To run the two tests, HQ must authorize a separate licensed Windows Unity **6000.3.14f1** disposable workspace (NOT the Codex active worktree). Copy only `B0161CrossFeatureRegressionTests.cs` into the compatible original existing EditMode assembly, run Unity Test Runner or `-runTests -testPlatform EditMode -testFilter B0161CrossFeatureRegressionTests`, retain original NUnit XML, Editor.log, exit status, Unity version and full source SHA. B01-61 does not claim native execution or PASS.

This fixture intentionally does not re-report B01-61's prior weapon-detach/remount policy risk (R07) or B01-60 D04 camera-loss finding. It is **not** the CHL/PART G1 gate, G2 permission, CODEX_ACK, or merge approval. No canonical gameplay files are changed.

## PASS13 — original NUnit/Player release evidence: fail closed

`python3 Tools/B01/B01-61-PASS12/audit_original_nunit_xml.py --repo "$PWD" --sha 231005c6d0351945a8ee6684f3856af118f0e94f` reads the **actual Codex Git-committed XML byte content**; it does NOT run Unity. The two source XML files each contain invalid XML: literal `<WORKSPACE>` in attribute values (**EditMode: 2 occurrences; PlayMode: 3**), so strict Python ElementTree rejects the raw file. The verifier explicitly records `raw_original_xml_well_formed=false` and replaces only that known placeholder *in memory* for trustworthy bounded case enumeration; the original immutable byte error is never hidden or certified as valid.

After that disclosure, targeted original owner-run results show EditMode **51/51** from a discovered top suite with **1270** cases and PlayMode **27/27** from **474**. These are historical selected Codex tests on game SHA `7299dcc5fd63ac042a0ffcd97b69d02fb8744649`, not our B01-61 native execution. The XML probe reports coverage by actual executed test names for garage, map and MOV; D03 dual warnings, D04 absent-camera release, R07 B-only remount, and this cross-feature test all have **no exact direct test names in the targeted selection**. This does not claim no such tests exist anywhere in the repository.

A future full-release operator should first export **well-formed original XML** while preserving raw provenance, then execute a fully unfiltered exact-integration-SHA EditMode and PlayMode plus real Windows Player GUI and targeted negative cases. The gate `--require-full` currently **returns nonzero**, and the GitHub workflow verifies that it correctly rejects the partial original receipts (negative CI expected-block is PASS). **No full-suite approval, G1, G2, or new Codex gameplay fix follows from any of this.**

B01-61 owns only the QA artifact; Codex owns game and original test/evidence production. B01-60/41 may execute native QA only after explicit HQ licensing and isolated workspace permission.
