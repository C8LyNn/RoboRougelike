# B01-60 — PASS17 W19 | native receiver v2 for **sole selected R3 #642**

**HQ:** [#646](https://github.com/C8LyNn/RoboRougelike/issues/646). Sole owner B01-23 [#642 @ `22cec9888047994ae523cbe7bf1b0a483d64cec4`](https://github.com/C8LyNn/RoboRougelike/pull/642) selected as **R3 SOURCE BASE, NOT R3 G0 release** [exact owner decision](https://github.com/C8LyNn/RoboRougelike/pull/642#issuecomment-6530905217). Alternative [#644 @ `d9b95146e46a5d81d5a291d7887e456164f4d0c2`](https://github.com/C8LyNn/RoboRougelike/pull/644) ALT_HOLD; older #604 ALT_HOLD. Original frozen game [#510 `d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510), binary Save schema [#556 `33094f154c7b91119b36d58aab285e6e83c5de57`](https://github.com/C8LyNn/RoboRougelike/pull/556) and selected original R2 [#596 `efb2b06eafedab5469b57f163152d96aca3c5915`](https://github.com/C8LyNn/RoboRougelike/pull/596) remain immutable. No Codex code change, no unauthorized merge or physical canonical install.

## New W19 functionality (nonduplicated, scoped B01-60)

Reuses the already source-tested and hardened B01-60 [W18 native selected-host launcher #643](https://github.com/C8LyNn/RoboRougelike/pull/643) **without rewriting game logic or touching older PR**. W19 new `run.py` imports **the same** frozen W18 B01-60 launcher, replaces ONLY its QA runtime source pins for the owner-selected **original #642**, and requires EXACT **five** unchanged original #642 NUnit `[UnityTest]` PlayMode test methods. No #644 `HostSlotLease.cs`, `CheckpointFence.cs`, GUID, assembly, tests or source is imported. New QA files are exclusively `Tools/B01/B01-60/P17W19Selected642Native/**` and a unique workflow. Previous #643 baseline SHA `acddb0b5226df2354b2255e0af510e39fe464381` retained unchanged; five W19 files changed total. Pin original #642 C# Unity host Git blob `7b8354bc4cd846227b4bdf6d25d81bed34a325bc`, original R3 core `a0a1923216d88ff8f50c560196f82cdec694f15c`, original Unity PlayMode fixture `e398b1fde8dcbd8e6d30a65b8f4b91a4965b9dd8`, original #510/#556 blobs from prior receiver.

The **real native launcher** (if activated Unity legally available) reuses the W18 receiver to physically stage FIVE separate, randomly named QA-only `ProjectSettings.asset` company/product identities, with exactly the original #642 source and own pre-case `CleanPrivateCheckpointScope` NUnit guard. Each original case requires a genuine Unity 6000.3.14f1 Editor process on its isolated project with genuine separate `Editor.log`, exact NUnit fullname/`Passed`, zero other cases, exit0 and full post-Editor source identity. **10 native Editor processes** would be needed (5 private scope guards +5 original selected PlayMode). A successful selected-only receipt still CANNOT stand in for the original [#532/#601](https://github.com/C8LyNn/RoboRougelike/pull/601) 2 EditMode+14 independent PhysX PlayMode cases or real integrated Save. Do not claim G1 without both.

## NEW P0 source-derived native fixture generation mismatch (original R3 #642, NO producer edits)

The selected original #642 native `ColdRestart_RestoresRealRigidbodyAndConsumedIds` contains exact sequence:

```cs
// original selected #642 source; excerpt only
Assert.IsTrue(original.TryRestoreBytes(
    IsolatedTwinRigSaveHost.MakeFixture(41,4,true,false),41,4));
Assert.IsTrue(original.TrySaveBytes(out byte[] saved));
...
Assert.IsTrue(restored.TryRestoreBytes(saved,41,4));
Assert.AreEqual(4,restored.Generation); // P0: expected 4, but saved snapshot is gen 5
```

The selected original R3 `AtomicWorldSwap.TrySave` uses `long next=newest+1;` and encodes `new RunSnapshot(snap.RunId,next,snap.Parts)`. After successful generation-4 Load, the actual new Save snapshot carries generation **5** and a successful Restore hydrates **5**. But original immutable PlayMode test expects **4**. It will not correctly validate native restart until B01-23 supplies an authorized NEW R3-versioned native fixture correction and native source QA/G0 are repeated against that exact new SHA. **This is an original-source test-consistency finding, NOT an observed native Unity failure**. It also does not prove the R3 ambiguous post-commit P0 is fixed. No permission to edit frozen producer tests.

## Executed tests / stop rules

Source QA only, physically stage then safely rollback FIVE original selected #642 standalone Unity project identities and enforce **11** fail-closed original-source violations: tampered #510 private ledger, #556 codec, selected #642 host/core/fixture, deleted private test guard, injected losing #644 HostSlotLease.cs, foreign EMPTY directory, altered QA project identity, changed ProjectVersion, and mutation of the native fixture generation assertion. Extra original #642 source core/host/test byte Git pins and exact five test names checked. Neither alternative is imported. Running:

```sh
python Tools/B01/B01-60/P17W19Selected642Native/test_source_fixture.py
python Tools/B01/B01-60/P17W19Selected642Native/run.py --mode preflight --out /absolute/new/output
```

Real authorized Unity (only if legally activated; **not available on current B01-60 host**):

```sh
python Tools/B01/B01-60/P17W19Selected642Native/run.py --mode native --unity /absolute/Unity6000.3.14f1 --out /absolute/new/native-output
```

The original W18 runner refuses missing Editor with exit **3**, never creates genuine native XML or Editor.log; actual present Editor uses real `-runTests`. No synthetic process/subprocess/XML is used to claim any native G1. **Even if** a selected #642 native test somehow reports passed, R3 native RELEASE must HOLD until independent P0 fixes and #510 integrated APIs reviewed. Real source-only CI Windows2025+Ubuntu24.04 produces source receipts, never source-only `G1=PASS`.

**Gates:** old #510/#556 source G0 accepted only, historical frozen #596 physical G0 SOURCE_ONLY accepted, **new selected R3 #642 G0 PENDING** independent B01-40/62, independent R3 host security HOLD (#637 ambiguous postcommit, owner W19 separate fix), R3 native fixture P0 SOURCE_RISK, real Unity6000.3.14f1/PhysX Edit/Play `G1_NOT_RUN`, LIVE_CANONICAL_SAVE_HOLD (private original #510 Rig[]/ledger owner API missing), G2 HOLD, Codex ACK NONE, not merged or released. Older #643 own W18 PASS remains a separate #596-specific historical source receipt, NOT carried to #642 automatically.

**Six lifecycle states:** `IMPLEMENTED=YES`, `TESTED=SOURCE_ONLY_ONCE_CI_RUN`, `QA_ACCEPTED=NO_NATIVE`, `READY_FOR_INTEGRATOR=NO`, `INTEGRATED=NO`, `RELEASED=NO`. Integration feedback `NOT_MEASURED`.

**Receivers:** B01-23 sole owner next version fixes the original #642 native generational assertion and original ambiguous-durable postcommit fault/lease lifecycle, B01-61/41 exact core+public host regression, B01-40/62 formal physical original R3 G0, B01-60 only an activated Unity host may generate true G1 with old original 16+selected 5+5 guards, B01-00 sole Codex Inbox #79. **Rollback:** delete only five new W19 B01-60 source/QA paths; keep frozen #643 and #510/#556/#596/#642 source untouched.
