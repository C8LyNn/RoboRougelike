# B01-23 · PASS17/W20 · Canonical World API 001 / owner #666

**STATUS: isolated executable NEW owner API source; NOT canonical #510 integration.** Parent explicitly sole selected R4 [#647 @ 09c1809b73324a6d31c13e5d0143de62ac1e3ca6](https://github.com/C8LyNn/RoboRougelike/pull/647). #652 is ALT_HOLD; #644/#604 ALT_HOLD; original frozen #510 d94400d95d13db7f27e1533df05c9292d5380431, only original #556 33094f154c7b91119b36d58aab285e6e83c5de57, old #596 efb2b06eafedab5469b57f163152d96aca3c5915 and selected R3 #642 22cec9888047994ae523cbe7bf1b0a483d64cec4 all unchanged.

## Actual NEW portable API and explicit boundary

- Unity-compatible C#9 **CanonicalWorldGateway** is new, separate from #510, and directly uses original selected #556 `SnapshotCodec` and `ColdRestore`, which rebuilds original W06 consumed membership with stable Light/Heavy IDs 2310001/2310002. One main-thread coordinator, quiescence gate, reject wrong run/stale generation/corrupt payload/duplicate or foreign stable identity before staging.
- `ICanonicalMutationGate` must be implemented by real authorized Unity game host to freeze all detach/reattach, AI, GUI and real Rigidbody/physics writers. `ICanonicalRigFactory.BuildInactive` must be implemented in NEW owner-approved #510 extension to create **real Rigidbody/GameObjects and private R2 `AtomicDockLedger`** from validated original #556 cold ledger. `ICanonicalStagedWorld.Capture` must return actual real parent/pivot/position/velocity/kinematic/private ledger consumed state. **No reflection** into frozen source, no fake direct private field access. Until this future port exists this is an executable **pure-source prototype with a test double**, NOT in-game SaveLoad.
- Source transaction stages inactive world, verifies original exact #556 encoded physics/ID before and after Activate, deactivates old world and commits through `ICanonicalCommitPort` supplied by B01-62. If Commit could already be durable and throws after lost ACK, SAME-CALL sticky `RollbackQuarantine`, compensated old scene cannot Save. Failed cleanup/rollback also immediate quarantine. After full physical readback, publishes pointer and retires prior world. Quiescence and reentry checks fail closed. `TryCapturePreview` **IS NOT a committed save**, intentionally no automatic disk writing, generation bump or claims about crash durability.
- B01-62 must supply **separate robust durable single-slot complete snapshot+revision atomic recovery** before real saving; the source tests' `Disk` class is deliberately **NOT crash-safe** and uses a physical temporary test file solely to reproduce failure paths. No source claim of power-loss safe journal, two-phase commit, directory fsync, signature/authenticity, anti-cheat or concurrent multi-host owner lock. Only #556 wire schema selected.

## Real original C# source testing

`dotnet run --project Tools/B01/B01-23/P17W20CanonicalSaveApi001/CanonicalApiSourceTests.csproj -c Release`

Compiles exact immutable original #556 DetachedSnapshotContract.cs and W06 DebrisImpulseCore.cs directly together with new API, C#9, warnings as errors on Windows+Ubuntu. Tests all four Light/Heavy consumed combinations, physically written OS-disk authority and lost ACK, foreign/wrong run, original SHA/ID, 16 different checksum corruptions, postactivation mutation, rollback quarantine, precommit fault control, cold restart, wrong thread and idle reentrancy. **Not real UnityEngine compilation; genuine Unity test must be delivered by B01-60 separately.**

## Original Git source manifest / safe receiver

`python -m unittest discover -s Tools/B01/B01-23/P17W20CanonicalSaveApi001 -p test_package.py -v`

`python Tools/B01/B01-23/P17W20CanonicalSaveApi001/package.py build --archive dist/B0123CanonicalApi001_Original.zip`

Only this separately named `Assets/B0123CanonicalSaveApi001/**`, own `Tools/B01/B01-23/P17W20CanonicalSaveApi001/**` and its own CI workflow; four original Unity GUIDs, one own asmdef. Manifest pins exact source SHA/ancestry and real original #556+W06 Git blobs, no #652/#644/#604 sources, forged native G1/ACK rejected. Stage only NEW owner scratch receiver, refuse symlink and pre-existing target; rollback only manifest-owned unchanged bytes and do not remove foreign EMPTY directories/paths.

## Gates / owners

B01-23 owner API draft author, B01-62 supplies separately independently tested full snapshot+checkpoint journal and recovery, B01-20 defines visible save states/feedback to user, B01-61/41 original-source fault test, B01-40 formal exact manifest G0 and safe disposable receiver, B01-60 Windows Unity6000.3.14f1 runner [#667](https://github.com/C8LyNn/RoboRougelike/issues/667) with real original test XML+Editor.log (otherwise NOT_RUN), B01-00 only eventual Codex intake. NO merge, NO Codex ACK, no real #510 port implemented yet, no G1 or G2 release.
