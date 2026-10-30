# B01-60 — PASS17 WAVE18 — ONE selected #596 / private per-native-case Unity projects

## Scope, freezes and exact source

[HQ #632](https://github.com/C8LyNn/RoboRougelike/issues/632). **ONLY selected original B01-23 producer [#596](https://github.com/C8LyNn/RoboRougelike/pull/596) exact `efb2b06eafedab5469b57f163152d96aca3c5915`**. Original frozen game [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) `d94400d95d13db7f27e1533df05c9292d5380431`; original frozen Save codec [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) `33094f154c7b91119b36d58aab285e6e83c5de57`. Losing #604 `1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a` = `ALT_HOLD` (NOT installed, NOT imported, NOT cherry-picked). This is **new B01-60 QA-only source**, stacked on frozen [W17 #621](https://github.com/C8LyNn/RoboRougelike/pull/621), original SHA `f4e26ac21ade9a54c0c38927ee48cea50ed8377c`. Existing B01-60 hardened native #532/#542/#567/#581/#601 source and selected owner source are untouched. No main/develop merge, Codex gameplay edit or canonical Save/Load integration.

## Why this is new (not duplicate W17, #631, #629, #628 or B01-40 G0)

W17 #621 independently verified original Git identity of four `[UnityTest]` methods, and discovered the source-derived **shared checkpoint path** risk. B01-41 #631 and B01-61 #628/#629 independently target original source host concurrency/lease; B01-40/62 target original physical producer ZIP & G0. **W18 builds and physically stages the FIRST specifically selected #596 dedicated NATIVE TEST receiver**: it assembles immutable original actual Unity C# assets plus package/version metadata from #596, not a competing core-only fake or reimplementation, and provides an actual executable licensed-Unity runner that can run selected `IsolatedSaveWorldNativePlayModeTests` as **FOUR SEPARATE native Unity processes and project identities**. This is not a second producer Save/Load implementation or duplicate original 16-case #532 runner.

The original #596 Unity host uses one `Application.persistentDataPath/b0123_one_save_host_v2.checkpoint` for all original NUnit fixture GameObjects. Original tests use different run IDs 31/41/9; a shared Editor project/cross-test checkpoint can cause contaminated native testing. W18 avoids relying on `TearDown` that deletes player data: it uses a **fresh disposable per-test project**, patches ONLY its copied `ProjectSettings/ProjectSettings.asset` `companyName` and `productName` to unique random B01-60-QA-only identities, and injects ONE QA-only `[Test] B0160W18PrivateCheckpointProbe.CleanPrivateCheckpointScope`. This probe MUST succeed in a SEPARATE Unity PlayMode process **BEFORE** the original exact selected native test. The probe asserts the actual Unity `Application.persistentDataPath` contains its private product identity and has NO existing checkpoint sidecar, before original selected public MonoBehaviour can be instantiated by its real test. If Unity chooses a different persistence root or does not isolate correctly, **FAIL_CLOSED**; NEVER erase/reuse real project or user save. A random product slug per invocation and FOUR distinct project IDs guarantee four separate checkpoint scopes at the test-harness level, contingent on probe actually passing under real Unity. The original selected #596 source/test bytes, Unity .meta GUIDs and frozen game/save source are never edited. New QA probe + project identity adjustments live ONLY in temporary source-locked QA receiver; this is not an in-game test install.

Native selected host test identities **from the original #596 source**:
1. `ColdLoad_RealRigidbody_RoundTripLightHeavy`
2. `CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched`
3. `ColdRestart_RestoresRealRigidbodyAndConsumedIds`
4. `WrongGeneration_CannotPartiallyPublishRealPhysicalWorld`

Each original case must have genuine Unity6000.3.14f1 Editor.log/NUnit XML + actual return code 0, ONLY exact one filtered selected-original NUnit fullname PASSED, no other failed test, post-Editor `Assets/**` and `ProjectSettings.asset` byte identity. **Eight real Unity subprocesses are required (4 guards +4 selected tests).** Even if these eight succeed, that verdict is `PASS_SELECTED_ONLY`, NOT full G1; the existing hardened [#532+#567+#581+#601 runner](https://github.com/C8LyNn/RoboRougelike/pull/601) must ALSO be executed with its original 2 EditMode +14 independent PhysX PlayMode tests and genuine evidence. Selected isolated host tests do **not** access the private original #510 `Rig[]/AtomicDockLedger` canonical runtime. All native evidence is `G1_NOT_RUN` until actual activated Unity executes it. Native version checking is based on actual Editor.log and is not cryptographic attestation.

## Commands / real gate semantics

Preflight only (safe real original Git source physical staging+rollback; **does NOT invoke Unity**):
```sh
python Tools/B01/B01-60/P17W18Selected596Native/run.py --mode preflight --out /absolute/NEW/qa-w18-preflight
python Tools/B01/B01-60/P17W18Selected596Native/test_preflight.py
```

Native (requires legally activated local Unity 6000.3.14f1; no remote Unity licensing bypass):
```sh
python Tools/B01/B01-60/P17W18Selected596Native/run.py --mode native --unity /absolute/path/to/authorized/Unity --out /absolute/NEW/qa-w18-real-Unity
```

Missing/nonexistent Editor returns **exit 3**, physically builds source project before declaring `G1_NOT_RUN`, does not produce real NUnit XML/Editor.log. Actual process/nonzero XML/log/identity/isolation/source contamination returns **exit 4** / fail-closed with evidence only; no `native-verdict` claim. When all four original selected cases pass, writes `selected-bridge-native-verdict.json` with explicit `selected_native=PASS_SELECTED_ONLY`, original independent 16=**REQUIRED_SEPARATELY**, canonical G1=**NOT_VERIFIED_BY_SELECTED_BRIDGE_ALONE**. No background execution or fake engine. CI without Unity runs source preflight and explicit missing-Editor negative only, **not actual G1**.

## Source-only negative QA with actual physical filesystem

`test_preflight.py` calls the original existing hardened B01-60 receiver `stage / require_native_source_integrity / rollback` against exactly selected #596 source. Builds **FOUR real disposable Unity projects** with four distinct protected application identities/probe source and executes full **original-byte exact allowed-only safe rollback**. Then on ten OTHER throwaway projects physically injects tampering (frozen V2 game/ledger, frozen #556 codec, chosen host C#, deleted selected core, foreign C#, missing new checkpoint probe, foreign empty folder, altered private project settings, changed Unity version file, altered manifest), and requires fail-closed. All adversarial samples exist only in disposable folders; no selected #604 code is installed, no unsafe deletion of shared or user files.

Both Ubuntu24.04 and Windows2025 source-only GitHub Actions run these tests and native-missing-Editor exit3, preserving own receipts labelled `NOT_RUN`. Only after successful exact-SHA CI may `B01-60_W18_SOURCE_RECEIVER_PREFLIGHT_PASS` be claimed.

**Gates and receivers:** frozen #510/#556 G0 accepted SOURCE_ONLY; formal selected #596 new physical G0 B01-40/62; W18 B01-60 receiver source QA separate; **native selected-only and original PhysX G1 NOT_RUN** on current host, no Unity license/Editor, no genuine XML/log; live playable Save/Load HOLD, canonical private ledger API not owner-authorized, G2 HOLD, Codex ACK NONE, no merge. B01-23 owner receives checkpoint-isolation findings/new-version test inputs; B01-61/B01-41 independent source host/lease QA; B01-40/62 original source receiver/G0; B01-60 activated Unity operator; B01-00 sole #79 Codex interface.

**Rollback:** discard only the FIVE new W18 B01-60 files (tool/README/manifest/CI, standalone QA runner/test) on W18 branch. Frozen W17 original B01-60 receiver and original #510/#556/#596 remain byte-identical.
