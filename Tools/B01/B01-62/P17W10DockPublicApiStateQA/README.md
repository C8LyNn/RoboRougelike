# B01-62 / PASS17 WAVE10 — DOCK-PUBLIC-API-STATE-QA-001 R1

**Owned scope:** this directory and its dedicated Actions workflow only. Based on frozen B01-23 #471 exactly at `763748f1be0c61356fb4462144640bb359364226`; no B01-23 source, Codex, main/develop edits. Do not merge.

## Why this is different from #472 and #476

#472/#476 proved that original W07/W08 GUI bypassed W08 optional clearance. W10 checks the **new #471 W09 implementation itself**, including (a) both old behaviours disabled at arm and re-disabled in Update, (b) W09 all-layer + W08 double audit before demo reattach, (c) original public W07 `Reattach(int)` STILL callable outside that W09 GUI, (d) exactly when old W07 re-arms consumed stable part identity, and (e) fail-closed negative mutations of the new W09 host source. A positive CI test validates the **existence of the remaining external API blocker**, not gameplay safety.

## Reproduce

On the isolated W10 branch with Python 3.11+ and Git:

```bash
python -m unittest discover -s Tools/B01/B01-62/P17W10DockPublicApiStateQA -p 'test_gate.py' -v
mkdir -p /tmp/b0162-w10-receipt
python Tools/B01/B01-62/P17W10DockPublicApiStateQA/gate.py --repo . --receipt /tmp/b0162-w10-receipt/evidence.json
python Tools/B01/B01-62/P17W10DockPublicApiStateQA/gate.py --repo . --receipt /tmp/b0162-w10-receipt/release.json --release
```

The **release command MUST exit 3** for original #471; treat this as a real `HOLD_UNSAFE_PUBLIC_API` release verdict, not failed QA execution. Real original 6 Git blob identities are checked before any assertion. Evidence JSON is deterministic, write-once, UTF-8/LF. The GitHub workflow runs positive and 26 negative/positive tests on both Windows 2025 and Ubuntu 24.04 and uploads raw stdout and receipts.

## Exact original source invariants and receiver

W09 is a **source-level protected isolated demo UI**. A separate external script still can call the unmodified public W07 `DebrisRigPlayground.Reattach(int)`, which repositions Rigidbody and rearms the consumed ID without obtaining W09 `Inspect` clearance. W09 disarming the GUI does not imply that this public method became safe. W09 source `TryDock` does check permission first; it is **not accused of unguarded reattachment**.

**Required owner fix:** B01-23 must issue a **new immutable version**, without altering W06/W07/W08/W09 frozen commits, that enforces fail-closed actual collision/reach checks for **every entry point that can rearm the stable ID** (including public method or authoritative host adapter); enforce no state mutation for blocked, dead, kinematic or non-detached parts. Native Unity PhysX EditMode/PlayMode, script execution order and real persistent save/load are separately required. B01-62 #457 original ledger restart test remains evidence that there is no approved production save schema; W10 does not duplicate that persistence harness.

**Gate results:** independent W10 QA `SOURCE_EVIDENCE_PASS` if workflow green, `EXTERNAL_PUBLIC_API_HOLD` until producer fix, `NATIVE_UNITY_NOT_RUN`, `G1/G2_HOLD`, `CODEX_NOT_ACK`. Receiving roles: B01-23 source owner, B01-40 physical/GUID/asmdef gate, B01-60/61 independent licensed-native regression, B01-00 future #79 routing. No QA-only guard should be installed into live game.

**Rollback:** close draft and remove ONLY W10 QA paths from this branch. Original #421/#439/#455/#471 source is immutable. There is no Unity gameplay installation or save migration to undo.


## W10 extension — secure V2 #488: transaction and consumed-ID safety

**Immutable new producer candidate:** [#488](https://github.com/C8LyNn/RoboRougelike/pull/488) `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3`. This is distinct from prior independent [#487](https://github.com/C8LyNn/RoboRougelike/pull/487), which only inspects the older W09 public-call bypass. The W10 extension checks a **different source and data-integrity invariant** in V2, without editing V2.

- `v2_transaction_gate.py` directly fetches **five exact original Git source objects** W06/W10, including the real original V2 `CommitDock`, `GuardedDockCommand`, W06 ledger and emitter, and V2 scene builder. The three public V2 docking aliases are routed through one Inspector before Commit. However the V2 private `CommitDock` mutates velocity, Rigidbody kinematic flag, parent, local position and detached flag **before** testing `RearmAfterReattach`, which can fail when the consumed ID is absent. Source review flags `HOLD_COMMIT_BEFORE_ID_REARM`.
- `test_v2_transaction_gate.py`: 14 independent original-source pin/contract and negative mutation checks (not original engine execution).
- `V2_StateQA.csproj` / `V2_StateProgram.cs`: C#9/.NET8 directly compiles and executes the **unmodified original W06 DebrisImpulseCore.cs and original V2 SecureDockPolicy.cs**, staged from the exact V2 source Git SHA, not mocks. Nine checks prove consumed-ID loss after original `ClearForNewRun()`, false rearm in that state, actual guarded router committing after allowed inspector, rejection on blocked aliases and an explicitly **MODELED physical state sequence** that remains modified when the ledger rejects. The state model is NOT Unity Rigidbody execution, and original production `CommitDock` is source-inspected, not executed in .NET.

Reproduction on a disposable branch checkout with Git + Python3.11 + .NET8:
```bash
git fetch --no-tags --depth=1 origin 33a32b8bd9ab40467ab1a17b5684926a37dbd4d3
python -m unittest discover -s Tools/B01/B01-62/P17W10DockPublicApiStateQA -p test_v2_transaction_gate.py -v
mkdir -p Tools/B01/B01-62/P17W10DockPublicApiStateQA/.output
python Tools/B01/B01-62/P17W10DockPublicApiStateQA/v2_transaction_gate.py --repo . --receipt Tools/B01/B01-62/P17W10DockPublicApiStateQA/.output/v2-evidence.json --stage Tools/B01/B01-62/P17W10DockPublicApiStateQA/.source_v2
dotnet run --project Tools/B01/B01-62/P17W10DockPublicApiStateQA/V2_StateQA.csproj -c Release
```

Write-once stage/output directories: remove only those disposable **QA-generated** directories on rollback; never run any game-save mutation. Native licensed Unity tests remain **NOT_RUN**. **B01-23** should accept/reject the source precondition defect with a separately versioned owner fix; ideally enforce consumed-ID preconditions before any rigidbody/parent modification and test failure atomicity after a valid detach and ledger reset, with no unsafe double rearm. B01-60 runs real Unity PlayMode, B01-40 formal G0; B01-00 alone queues any future Codex integration.
