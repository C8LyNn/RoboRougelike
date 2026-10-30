# B01-23 — PASS17 W11 / SECURE-REATTACH-V2-R2 — atomic dock, selected #488

**SELECTED only #488** source replacement scene/control path. **DO NOT INSTALL #494** alongside it. Old immutable #488 HEAD: `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3`. R2 is a separate versioned descendant; original W06 #421, #439/#455/#471 and Codex canonical files remain unchanged.

## RED and source fix

Independent B01-62 [#486](https://github.com/C8LyNn/RoboRougelike/pull/486) reproduced the old V2 `CommitDock` ordering: velocity/kinematic/transform/Detached mutated BEFORE `Emitter.RearmAfterReattach`, allowing a lost consumed ID (original W06 `ClearForNewRun`) to throw after partial physical state changes.

- NEW original `AtomicDockLedger.cs` owns **unaltered original W06** `DebrisImpulseLedger` internally, one per V2 rig. No publicly accessible W06 emitter reset. Real physical detach uses original `DebrisImpulseLedger.TryDetach` and `Rigidbody.AddForce(...ForceMode.Impulse)`.
- Reattach, TrySafeReattach, ReattachFromGui and GUI all still go through same original V2 `GuardedDockCommand` + all-layer `Physics.OverlapBoxNonAlloc` and `ComputePenetration`. Private `CommitDock` now snapshots body velocity/angular velocity/kinematic, parent/pose and Detached. Ledger missing ID -> false **without any physical mutation**. A consumed ID is rearmed **before** physical mutation; on exception rollback restores all physical data and reconsumes original ID with zero impulse. A secondary failure of rollback triggers explicit quarantine exception, not an unsafe PASS. Reset cannot clear any ledger while transaction active.
- `GuardedDockCommand.LastResult` now reports `DockResult.TransactionRejected` when ledger or physical callback refuses. Old source was misleadingly Allowed for failed commit.
- New native PlayMode regression authored with genuine Rigidbody and forced private ledger reset through test-only reflection; tests all three public methods for no parent/velocity/kinematic/Detached changes. Native Unity **NOT_RUN**; no engine activation credentials.

## Reproducible original source and ZIP tests
```sh
dotnet run --project Tools/B01/B01-23/P17W11DockAtomicR2/AtomicSourceTests.csproj -c Release
dotnet run --project Tools/B01/B01-23/P17W10SecureReattachV2/DockV2SourceTests.csproj -c Release
python -m unittest discover -s Tools/B01/B01-23/P17W10SecureReattachV2 -p test_package.py -v
python -m unittest discover -s Tools/B01/B01-23/P17W11DockAtomicR2 -p test_atomic_package.py -v
python Tools/B01/B01-23/P17W10SecureReattachV2/package.py build --archive dist/B0123_SecureReattach_R2_Source.zip
```

Versioned original Git physical ZIP contains cumulative selected V2-R2 source (V2 + original W11 source/tests + 2 workflows) and per-file exact SHA256/bytes/provenance. Original W06 core/emitter/asmdef Git pins, W10 superseded source SHA, 14 distinct Unity GUIDs and three asmdefs are validated. Staging only to NEW external disposable scratch, rollback refuses dirty/missing/foreign files and never rewrites canonical Codex/source game.

**Receivers:** B01-40 formal physical G0/rollback; B01-61/62 independent #486 RED→GREEN test on new exact SHA; B01-60 licensed native Unity6000.3.14f1 real PlayMode/PhysX G1; B01-00 only later queue #79. Old W07 scenes remain unsafe until authorized migration/removal. No claim this R2 automatically replaces canonical game or achieves native PASS. ACK_NO, G2 HOLD, NO MERGE.
