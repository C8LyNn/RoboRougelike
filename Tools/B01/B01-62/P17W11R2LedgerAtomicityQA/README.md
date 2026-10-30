# B01-62 — P17 WAVE11 — R2-ORIGINAL-LEDGER-ATOMICITY-QA / 1.0.0

## Purpose / ownership

Independent **data-integrity regression QA** for the two mutually exclusive B01-23 safe docking proposals: [#488](https://github.com/C8LyNn/RoboRougelike/pull/488) and [#494](https://github.com/C8LyNn/RoboRougelike/pull/494), dispatch [#500](https://github.com/C8LyNn/RoboRougelike/issues/500). **Do not install both.** Existing B01-62 [#486](https://github.com/C8LyNn/RoboRougelike/pull/486) already provides actual W06 ledger + V2 #488 modeled state witness and is NOT modified or duplicated here. This W11 package independently **compiles and calls the original #494 `DebrisRigPlayground` C# production class together with the unchanged original W06 `DebrisImpulseLedger` CLR implementation** and a strictly QA-only Unity API substitute. This is substantially more specific than synthetic transaction state modeling; still **NOT actual Unity, real PhysX, Player or persistent save/load.**

## Original immutable pins / exact source integrity

- #494 revised W07 public source `6efe45961ed0ffdf183e8016e350978b9f3dc51c`, blob `60fc22efec0e49d7688adde1fcb776093f2196af`.
- #494 original dependent W07 `RigDemoDefinition.cs` blob `b7d1a503c89537316fe48dbb54f5997bb21d14ce`.
- #421 original W06 `DebrisImpulseCore.cs` blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`.
- #421 original W06 `DebrisRepulsionEmitter.cs` blob `2b3a8465c550bd49a5c98be5a8064a4ce0947f4b`.
- #494 producer QA `UnityBoundaryStub.cs` blob `d5fc14c1ad7d4ce730f03aef22a4272cb6dd7e43` (**test-only shim**, NOT native engine).
- #488 original replacement `SecureDebrisPlaygroundV2.cs`, SHA `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3`, blob `491c89a18827bb027854bba5e6be6630ce4947b6`.

`verify.py` uses `git show` to read actual original blob bytes from precise commits, independently recomputes Git blob SHA1 and verifies `git rev-parse ref:path`, and examines actual original mutator order for BOTH #488/#494. It refuses source drift and refuses any known new atomicity protocol until fresh review. Runtime source snapshots are generated only in ignored CI scratch `SourceSnapshot/`; none of the original Git branches are mutated. Machine-readable receipt states `BOTH_CANDIDATES_TRANSACTION_HOLD`.

## Actual runtime-source witness

`Program.cs` and `LedgerAdapter.cs` compile against:
1. **Unchanged original #494 game class** `DebrisRigPlayground.cs` as fetched, which has an actual mandatory all-layer physical gate.
2. **Unchanged original #421 `DebrisImpulseLedger`**, with true `TryDetach`, `Contains`, `RearmAfterReattach`, `ClearForNewRun` C# behavior.
3. A TEST-ONLY managed Unity API boundary derived from producer's pinned test stub. Its old fake emitter class is stripped, replaced by the separate adapter that delegates all ledger ownership to actual unchanged W06 ledger. Physics overlap and GameObjects are simulated, so this cannot be called native Unity/PhysX.

Two physically different authored rigs are represented by the actual original C# mass/part definitions (light 1.5kg and heavy 12kg). Positive: detached consumes ID; clean reattach rearms ID once, subsequent detach possible. Negative: a simulated collider blocks with no state/ledger mutation. **Critical witnessed sequence:** original `Detach` -> external/public W06 `ClearForNewRun` while rig is detached -> remove any blocker -> original production `Reattach` passes the physical-gate shim -> original `RearmAfterReattach` returns false -> production throws **after** writing linear/angular velocity, kinematic, parent, mount position and `Detached=false`. Real class state is inspected (not merely copied in a fake C# state model). The test records the intentional failure as expected `RELEASE_VERDICT=HOLD_PARTIAL_STATE_MUTATION`, not a green product acceptance.

A second source-only gate establishes the SAME commit-before-ledger order in #488, as already separately observed in #486. **Neither proposal as pinned is transaction-safe**, even if their public API guard improvements address the original bypass. Host migration must pick ONE and obtain a separately versioned producer repair plus independently verify the rollback/atomic precondition and old legacy scene removal.

## Reproduction, evidence and rollback

On Python 3.12 + .NET8 + Git with original refs fetched:

    python -m unittest discover -s Tools/B01/B01-62/P17W11R2LedgerAtomicityQA -p test_verify.py -v
    python Tools/B01/B01-62/P17W11R2LedgerAtomicityQA/verify.py --repo . --dest Tools/B01/B01-62/P17W11R2LedgerAtomicityQA/SourceSnapshot
    dotnet run --project Tools/B01/B01-62/P17W11R2LedgerAtomicityQA/QA.csproj -c Release

Run in a disposable clone. Never overwrite an existing `SourceSnapshot` directory. CI provides Windows2025 and Ubuntu24.04 original-source code execution and raw receipt artifacts. **No installed Unity runtime, no GameObject PhysX, no real save writes, no native tests; G1=NOT_RUN.** Rollback = discard only this isolated B01-62 QA branch and its test folder/workflow; do not edit other PRs or canonical saves.

## Receiver / gates

- **B01-23:** select only #488 or #494; new immutable version must prove consumed-ID ownership/availability and *failure atomicity* **before** changing body/parent/detached state, or genuine rollback of all fields and ledger on ANY failure; no check-then-rearm race; enforce all old public routes and migrate host without legacy W07 usage.
- **B01-61 / B01-60:** new independent regression tests (ID missing after out-of-band ClearForNewRun, both rigs, all public aliases and GUI) and real authorized Unity 6000.3.14f1 Edit/Play/PhysX; assert no velocity, kinematic, position, parent, detach/ledger mutations on denial.
- **B01-40:** artifact and exact-SHA G0, migration/rollback, check no parallel installation of #488 and #494.
- **B01-00:** issue #500 decision and #79 Codex queue only when actual ACK; no implicit OFFERED/CODEX_CLAIMED/INTEGRATED.

**W11 is independent QA, not a game source repair. G0 = QA evidence only, candidate acceptance HOLD. G1 Unity NOT_RUN. G2 HOLD. CODEX_ACK NONE. NO MERGE.**
