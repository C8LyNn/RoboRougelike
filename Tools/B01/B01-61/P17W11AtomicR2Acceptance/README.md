# B01-61 — PASS17 W11 — independently executed Atomic Reattach R2 source acceptance

**Target:** B01-23 versioned producer **atomic R2** branch `b01-23/p17w11-secure-v2-atomic-r2` at exact source commit `d94400d95d13db7f27e1533df05c9292d5380431`, replacing unsafe frozen [#488](https://github.com/C8LyNn/RoboRougelike/pull/488) `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3`. Original W06 [#421](https://github.com/C8LyNn/RoboRougelike/pull/421) remains immutable `cb2b3a76e84266e2730fffc4ab048c1039f0652f`.

This is a separate independent QA green-retest based on original W11 R2, complementary to **[B01-61 W11 original RED #502](https://github.com/C8LyNn/RoboRougelike/pull/502)** that actually executed frozen W10 `SecureDebrisPlaygroundV2` plus original W06 ledger under synthetic Unity. It reproduced **six** physical partial-commit outcomes on **the unchanged original source**, two rigs × three entrypoints. That original bug history remains immutable.

## Actual original-code suite (NOT native Unity)

Compile exact original C#9 R2 `SecureDebrisPlaygroundV2.cs`, `SecureDockPolicy.cs`, `SecureRigSpec.cs`, NEW `AtomicDockLedger.cs` plus exact unchanged original W06 `DebrisImpulseCore.cs` (the real consumed-ID implementation). Only GameObject/Rigidbody/Physics APIs use an **explicit QA synthetic Unity shim**, not genuine Unity 6000.3 / PhysX. Runner exercises:

- **2 rigs × 3 public methods**: `Reattach`, `TrySafeReattach`, `ReattachFromGui`: with original source consuming ID, then `AtomicDockLedger.ClearForNewRun` while part remains detached, all six attempts MUST return false with `TransactionRejected`, without changing detached, parent, position, momentum or kinematic state.
- Positive original-source detach/dock/duplicate/detach controls for each route, and two obstacle-blocked controls with unchanged ID.
- Direct original atomic transaction failure injection: physical apply callback partially changes a synthetic state and throws; original R2 `AtomicDockLedger` must call rollback and reconsume original W06 ID with zero impulse. Retry then succeeds.
- Reentrant nested reattach and `ClearForNewRun` while ledger Busy fail closed.
- Explicit rollback failure MUST raise quarantine error and keep original consumed ID, not report success.
- Absent ID cannot invoke either physical callback.

`verify_source.py` validates 5 immutable original Git blobs, all 7 exclusively new B01-61 QA files, original rearm BEFORE apply, rollback, public-API routes. Windows2025 + Ubuntu24.04 CI archives actual original C# stdout. Unit success is **SOURCE QA only**, not native PhysX G1.

## Instructions

```sh
python Tools/B01/B01-61/P17W11AtomicR2Acceptance/verify_source.py
dotnet build Tools/B01/B01-61/P17W11AtomicR2Acceptance/OriginalR2AtomicHostQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W11AtomicR2Acceptance/OriginalR2AtomicHostQA.csproj
```

**Integration blockers:** B01-23 owns selecting ONE #488 R2 line versus #494; do not install both. B01-40 independently checks real physical package/ZIP/GUID/asmdef/rollback and single-variant staging. B01-60 must run ACTUAL licensed Unity 6000.3.14f1 EditMode/PlayMode/PhysX and distinguish full game migration from isolated demo. B01-62 must verify ID persistence and save/load. B01-00 alone controls Codex #79, ACK required; until then G2 HOLD, NOT_INTEGRATED, NO_MERGE. Old W07 unsafe public method still exists in legacy scenes; a new separate controller does not automatically migrate them.

**Rollback:** close unmerged independent Draft PR / drop `qa/b01-61-p17w11-v2-atomic-r2-current`, leave original producer and Codex code unchanged.
