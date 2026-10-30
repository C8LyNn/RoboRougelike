# B01-62 — P17 WAVE 15 / ATOMIC-HOST-PORT v1.0.0

## Only frozen dependencies

The **ONLY** selected producer data schema is [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) `33094f154c7b91119b36d58aab285e6e83c5de57`, actual C# Git blob `b52e511c370f5fca56c74029050844dc8cbb7f9a`, derived from original #549 but corrected for #550; **#549, #544, #554 HOLD**. The **ONLY** selected gameplay is [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) `d94400d95d13db7f27e1533df05c9292d5380431`, with real original W06 blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5` and actual original R2 AtomicDockLedger blob `b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9`; #494/#511 gameplay HOLD. This independent B01-62 package **never edits those versions or any Codex/current game runtime**.

## Implemented functioning host bridge contract

`FrozenSaveHostBridge.cs` is a **real, compilable C#9 Unity-facing HOST PORT** around actual #556 source. Its `TrySave()` validates host quiescence, captures an original `RunSnapshot` through explicit `IAtomicWorldHost`, uses the **actual #556 `SnapshotCodec.Encode`**, rechecks quiescence and unchanged world-token identity before returning fresh binary bytes. `TryLoad()` clones untrusted input bytes, calls the **actual #556 `ColdRestore.TryStage`**, checks run/generation, compares original W06 consumed-ID counts/identity, requires an offstage `IPreparedWorld` with freshly reconstructed per-rig ORIGINAL #510 `AtomicDockLedger` consumed state, rechecks host quiescence and world-token identity, then calls precisely one `TryPublish` **on the same synchronous Unity main thread**. The host MUST make publication an indivisible complete world-token swap after all physical bodies/transforms/ledgers are staged, with no live mutations during preparation. If publication throws or falsely returns rejection after mutating world token, the bridge calls `RestorePrevious` on the entire world. If rollback fails it quarantines rather than falsely returns PASS. Reentrant save/load is denied.

**Critical integration gap (P0):** frozen original #510 has private `Rig[]` and private per-rig `AtomicDockLedger`, with no owner-approved public capture, full offstage fresh-world creation or atomic restore/publication API. Thus **this source-executable bridge is not plugged into #510**, does not instantiate live Unity objects or change Rigidbody in #510 and CANNOT honestly claim a real-game Save→Load. Do **NOT** use reflection to access #510 internals, fake implementation of `IAtomicWorldHost` that mutates live Unity bodies during staging, or perform separate parent/body/ledger assignments while claiming atomicity. A separately versioned authorized **B01-23 / Codex host extension** must provide:

1. **CaptureCurrent** of both actual rigs with **original stable IDs/GUIDs**, true W06/R2 consumed ledger membership, Rigidbody world position, normalized rotation, linear/angular velocity, kinematic, parent, mass, session run/generation while *no detach, dock or reset is active*.
2. **PrepareFreshWorld** that instantiates inactive/offstage Unity host (new root, original 2 physical bodies, colliders, joints and NEW per-rig original W06/R2 ledger, explicitly constructed and consumed at zero impulse where detached), verifies original #556 logical state, no shared mutable token with running world and NO live physical mutations.
3. **TryPublish** that atomically exposes the complete staged world to simulation/players at the main-thread barrier, swapping *both rig roots AND per-rig ledger authority together*, with a strict false-return-no-side-effects contract.
4. **RestorePrevious** that restores the original complete physical world and ledger authority on ANY partial exception; if impossible, quarantine and require restart/repair. Must not report success for a partially restored world. Add tested exclusive ownership/lock to prevent concurrent callbacks.
5. Real persistent save transport: same-volume temp file, fsync+verified checksum, atomic rename, crash consistency and a unique writer lease. Do not confuse the binary source test saving temp files with the production checkpoint lifecycle.

The executable `Program.cs` uses a clearly declared **TEST-ONLY in-memory `FakeWorldHost`**, implements these **protocol guarantees with one managed reference swap** and compiles unmodified #556 + unmodified #510 original ledger + unmodified W06. It genuinely executes original two different Light/Heavy IDs/masses, writes the exact #556 binary to disk, restarts with new logical host per test, cold rehydrates original R2 consumed-ID ledgers, proves duplicate detach/rearm refusal, and checks a matrix of corruption, missing/foreign run, stale generation, busy/reentrant callbacks, mismatched staging ID, throws and after-commit false-return rollback. This is stronger than a copied state-machine fake, but is **NOT UnityEngine / PhysX G1**.

## Secure receiver migration and rollback

No competing installer or destructive rollback is introduced. For future authorized disposable Unity receiver use **B01-40 #536** source-verified directory-aware rollback with B01-40's exclusive target lease and preflight against legacy W07/#511/#494, missing/duplicate .meta GUIDs, foreign empty dirs/symlinks and modified bytes. B01-60 **#542** protects its **QA runner only**, not a parallel canonical runtime installer. #560 authoritatively checks selected #556 original physical ZIP/manifest/source-only G0, and #510 historical physical G0 remains from #521. **Never invoke original #510 destructive rollback on a shared folder.** All game receiver migration remains blocked until actual source owner adapter and native Unity G1.

## Test execution and rollback

On a disposable Git checkout with .NET8, Python3.12, Git, source SHAs fetched:

    git fetch origin 33094f154c7b91119b36d58aab285e6e83c5de57
    git fetch origin d94400d95d13db7f27e1533df05c9292d5380431
    python Tools/B01/B01-62/P17W15AtomicHostBridge/source_guard.py --repo . --out <new-own-Input-dir> --receipt <new-json-receipt>
    dotnet run --project Tools/B01/B01-62/P17W15AtomicHostBridge/OriginalHostBridgeQA.csproj -c Release

The CI executes original source on Windows2025 + Ubuntu24.04 (source QA), not the real UnityEditor. **G0 for this *new bridge* = PENDING independent B01-40 acceptance; G1 Unity6000.3.14f1 Edit/Play/PhysX = NOT_RUN; G2 = HOLD; ACK = NONE; integration = NO; Merge = NO.** Rolling back this PR means closing only this B01-62 Draft and removing own new QA paths; never change #510/#556 or Codex. Receiver B01-23 authorized new extension, B01-61 independent on actual extension SHA, B01-40 physical acceptance/rollback, B01-60 native with #532, B01-00 #79.
