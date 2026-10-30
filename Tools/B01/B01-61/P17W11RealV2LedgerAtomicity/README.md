# B01-61 WAVE 11 — direct original V2 Unity-host transaction failure witness

**One distinct regression** for [#500](https://github.com/C8LyNn/RoboRougelike/issues/500), not a duplicate of B01-62's modeled ledger report [#486](https://github.com/C8LyNn/RoboRougelike/pull/486).

**Original source pinned (read-only):** B01-23 V2 [#488](https://github.com/C8LyNn/RoboRougelike/pull/488) `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3` + original W06 [#421](https://github.com/C8LyNn/RoboRougelike/pull/421) `cb2b3a76e84266e2730fffc4ab048c1039f0652f`. This independent QA branch contains **seven new QA-only files**. Nothing in `Assets/**`, original producer branches, Codex, main/develop modified.

## New experimental distinction from #486

The `OriginalHostLedgerQA.csproj` uses exact original C# files by reference, not copied methods:

- **Original unchanged Unity `SecureDebrisPlaygroundV2.cs`** — the actual `Detach`, three public `Reattach` aliases, `InspectDock`, and private `CommitDock` execute;
- **Original unchanged `SecureDockPolicy.cs` and `SecureRigSpec.cs`**, plus **actual original W06 `DebrisImpulseCore.cs` and real `DebrisImpulseLedger`** execute;
- a **QA-ONLY SYNTHETIC Unity boundary** (adapted from B01-23 original #494 test stub) provides GameObject/Transform/Rigidbody/Physics interfaces and bridges original real W06 ledger into a test-only emitter. This is NOT real UnityEditor/PhysX and not a game source import.

`Program.cs` first proves the unchanged source succeeds for both light/heavy rigs and all three aliases with original consumed ID, and fails closed on an obstruction. For each of six cases (2 rigs × 3 entrypoints) it follows an *actual original execution* sequence:

```text
original V2 Detach() -> real W06 ledger ID consumed
original W06 ClearForNewRun() -> original ID removed while part remains detached
V2 public Reattach / TrySafeReattach / ReattachFromGui -> original InspectDock approves
original V2 CommitDock mutates physical Rigidbody velocities, isKinematic, parent, Detached
original W06 ledger RearmAfterReattach -> false; original V2 throws
QA verifies mutated physical state + zero consumed IDs despite exception
```

A QA PASS **means the transaction bug was REPRODUCED**, not that docking is safe. The runtime/test runner emits `SOURCE_RELEASE_GATE_RED=PARTIAL_COMMIT_BEFORE_LEDGER_REARM` and never claims native green.

### Reproduce

```sh
python Tools/B01/B01-61/P17W11RealV2LedgerAtomicity/verify_source.py
dotnet build Tools/B01/B01-61/P17W11RealV2LedgerAtomicity/OriginalHostLedgerQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W11RealV2LedgerAtomicity/OriginalHostLedgerQA.csproj
```

GitHub Actions runs actual C#9 + Python provenance on **Ubuntu24.04 and Windows2025**, archiving original output. Both original SHA pins and original W06 reset semantics fail closed on drift.

### Required owner correction and acceptance

B01-23 owns choice **ONE** of #488 / #494; independent B01-61 cannot select or rewrite producer source. Both W10 candidates perform mutation before ID rearm and need NEW version without altering frozen SHA. Owner must atomically claim/rearm once before Rigidbody/parent mutations, or implement a demonstrated rollback that fully restores Rigidbody/parent/position/velocity/ID on any rearm failure (not just `try/catch`). Must handle reentrancy and scene migration, save/loading precondition, old W07 route. **Receiver B01-40** formal G0 real ZIP and rollback, **B01-60** licensed Unity6000.3.14f1 EditMode/PlayMode/PhysX/native G1, **B01-62** stable-ID persistence and save/load, **B01-00** #79 queue after acceptance. Native Edit/Play/Player here **NOT_RUN**, G2 HOLD; no Codex ACK, no merge.

**Rollback:** close isolated Draft QA and delete `qa/b01-61-p17w11-real-v2-ledger-atomicity` only. No original V2 or W06 source changed.
