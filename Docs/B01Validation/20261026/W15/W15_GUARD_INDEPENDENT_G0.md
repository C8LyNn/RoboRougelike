# B01-40 W15 — independent original-byte ECON/LEGENDARY guard receiver G0

**Exact current producer**: [#263](https://github.com/C8LyNn/RoboRougelike/pull/263) `01ee22a3a2dbd0c2ced3140032042894f88d2ebb`. Previous #268/W14 package `e6c152f0795f105a94743018093b650d453e26a1` is **SUPERSEDED**; receipt rejects the old physical ZIP.

## Actual measured independent source inputs

* Current original producer Actions artifact **12654055980** / run [40329477269](https://github.com/C8LyNn/RoboRougelike/actions/runs/40329477269), physical inner ZIP **13 entries, 7908 bytes**, SHA256 `7f826d0148d0dacf612b256a21e32fe3eaaf341651c600ca742dd36e5fdcf09c`; externally stored manifest 13/13 original source bytes+SHA256 PASS; **9 Unity .meta GUID**, 2 asmdefs. Source `GuardedLegendaryPort.cs` Git blob `aa3094fdb0462e7a2b9f2576a8ae583b3c04ef2e` and six-test file Git blob `fc881ca4e263882aaa636d2a82c4432e36e2eff7` matched independently recomputed Git SHA1 of actual ZIP bytes against GitHub #263 source HEAD.
* LEGENDARY Unity original #242 `dffabbc6960304fdb33322b5a23c884feac804c6`, real ZIP 16 entries, 10959 B, SHA256 `14eccb49f69b2e67ecb34008fe19689b1b66e629ed94491224fb327216fc1fb2`: original external manifest **16/16 PASS**.
* ECONREBUILD original #252 `12572117a3dce369257feabc57021cde58023369`, real ZIP 16 entries, 21422 B, SHA256 `fdcb15e6b394715f450771cac798b1c02b8efe1f33acb815e9e0957c46bd2d87`: original external manifest **16/16 PASS**.
* Combined three original Unity packages: **43 unique Assets source files**, **29 unique GUID-to-path mappings**, **0 conflicting same-path bytes / GUIDs**; shared `Assets/B01Packages.meta` uses identical original bytes across producers, and on existing target always preserved.
* Assembly dependency closure from original asmdef JSON: `B01.EconLegendary.Guard` → `B01.Legendary.Unity` + `B01.EconRebuild.Runtime` **RESOLVED** in this three-package source stack.

## Actually executed working receiver transaction

Reused unchanged **PACKAGE-AUTO-001** implementation from [#238](https://github.com/C8LyNn/RoboRougelike/pull/238), not a copied algorithm. Three original source ZIPs verified **before** QA-only normalized combined source archive. Git created actual **detached, stamped disposable** worktree for a synthetic Unity6000.3.14f1/URP17.3.0 source fixture; original foreign root GUID preserved. Preflight passed; **42 source files physically staged and 42 physically removed by journal rollback**. Fault injection after fourth file automatically rolled back, 0 new source files remained. Wrong SHA, stale old original ZIP, deliberately forged SHA receipt, missing physical-reference identity interface, fake value equality replacing `object.ReferenceEquals` all rejected by regression tests. Original producer ZIPs were not republished as QA normalization.

**Test iteration:** First W15 local run 7 errors due to incorrect *receiver bridge* hash pin typed by auditor → fixed by re-reading actual original bridge receipt. Second run 1 test fixture failure because `_DISABLED` preserved checked substring → repaired negative test to remove interface. Third run **31/31 PASS** with old and current real Actions artefacts, including real worktree stage/rollback and crash recovery. No Unity runtime was executed by B01-40.

## Important unresolved receiver boundary

The producer's external `receiver-bridge-receipt.json` pins two patched receiver adapter hashes and marks `COMPILE_ONLY_RECEIVER_REFERENCE_NOT_INSTALLED`. **Those generated patched adapter bytes are NOT within the producer's Unity ZIP**. This gate independently checks the producer *receipt structure and pinned hashes*, not actual generated bridge file bytes or that the active Codex ports return one real live `R3VehicleModel` object. This is an explicit **remaining G0_TARGET integration proof**, not PASS. Caller must provide both adapters and original authoritative R3 owner object under *separately authorized isolated native QA*; reject cloned/stray objects, lost 0HP parts, and missing atomic reservation authority. No G1 claim on 20/20 producer C# surrogate or this Python source check.

**Gates:** `G0_SOURCE=PASS`, `G0_TARGET_LIVE=PENDING`, `G1_NATIVE=NOT_RUN_BY_B01_40`, `G1_INDEPENDENT_B01_60=PENDING`, `G2=HOLD`, `NO_MERGE`, `NO_ACTIVE_CODEX_GAME_EDIT`.

**Minimal next Codex/B01-60 QA-only request (NOT DISPATCHED):** after licensed disposable Unity6000.3.14f1 target HEAD ACK, stage exact #242 + #252 + #263, verify two patched adapters return *the same live* `R3VehicleModel` by reference and reject two distinct objects with identical physical GUIDs, collect 6 original EditMode NUnit XML/exit+logs and real 0HP reservation/replay/atomic failure telemetry. No host install until G2.
