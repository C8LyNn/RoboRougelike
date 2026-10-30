# B01-62 / PASS 17 / WAVE 11 — IMMUTABLE #494 PUBLIC-API + W06 LEDGER TRANSACTION QA

**Owner:** B01-62 independent Data Integrity & Save QA. **Target:** producer #494 at exact `6efe45961ed0ffdf183e8016e350978b9f3dc51c`, not an unrestricted floating branch. Comparison competitor #488 exact `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3`. Source parents are **never modified**. Git parent of QA branch is immutable original W06 #421, so no inherited W09 frozen-archive workflow is repackaged or falsely altered.

## Distinct verification, not another #486/#491

Prior independent B01-62 #486 executed genuine W06 ledger and V2 #488 guard, using a **modeled** physical mutation sequence. This W11 package executes the **actual unchanged #494 `DebrisRigPlayground.Reattach` Unity-facing C# class** compiled together with unchanged original W06 `DebrisRepulsionEmitter` and `DebrisImpulseCore`. The only simulated component is the low-level Unity/Physics API boundary, derived from original #494 producer's own test stub, with its **fake emitter removed** and minimum Rigidbody/attribute adapter implemented in an isolated disposable directory. This directly executes actual production `Reattach` ordering with the **real W06 ledger**, not a hand-copied mutation sequence. It is still **NOT native Unity, PhysX, PlayMode, Unity serialization or production save/load**.

**Test sequence:** build real two rigs -> detach (genuine W06 consumes part ID) -> call genuine public `DebrisRepulsionEmitter.ClearForNewRun()` while rig remains physically detached -> no contacts so producer mandatory all-layer guard allows -> call actual #494 public `Reattach`. Original source changes body velocity, angular velocity, kinematic flag, transform parent, position and `Detached` BEFORE W06 `RearmAfterReattach` fails. The original method throws with partial physical state; the source QA runner records an **expected RED release safety invariant**. Both light and heavy rigs independently checked. Additional tests prove clean dock, blocked physics fail-closed, legacy GUI respects mandatory guard and W06 refuses duplicate ID consumption.

**Frozen comparison:** immutable #488 also has rearm after physical changes; the Python comparator checks its unchanged blob for this ordering. **Neither frozen candidate is release-accepted** for failure atomicity unless an owner produces a separate versioned fix. This QA does not choose the winner for B01-23.

## Reproduce exact original sources with Git, Python3.11 and .NET8

```bash
git fetch --no-tags --depth=1 origin 6efe45961ed0ffdf183e8016e350978b9f3dc51c
git fetch --no-tags --depth=1 origin 33a32b8bd9ab40467ab1a17b5684926a37dbd4d3
python -m unittest discover -s Tools/B01/B01-62/P17W11Atomicity494 -p test_stage.py -v
python Tools/B01/B01-62/P17W11Atomicity494/stage_source.py --repo . --stage Tools/B01/B01-62/P17W11Atomicity494/.stage
dotnet run --project Tools/B01/B01-62/P17W11Atomicity494/Independent494.csproj -c Release
```

The dedicated GitHub Actions workflow performs that execution on both Windows2025 and Ubuntu24.04, captures raw logs, writes deterministic source blob provenance receipt and verifies cross-OS byte parity. `TOTAL 9 PASS 9 FAIL 0 V2_494_RELEASE_HOLD_PARTIAL_STATE` means **the QA detection works**, not that docking is safe. Any unexpectedly non-throwing result, missing original Git blob, missing source method, mutated dependence or changed public routing fails the QA build and requires review, never silent green.

## Required producer fix / formal gates

**B01-23** must select either new versioned V2 replacement (#488 line) or mandatory W07 API patch (#494 line), install exactly one in a disposable host, and enforce atomic rearm eligibility **before** Rigidbody/parent mutation (or a provably atomic rollback preserving detach and ID state). A preflight `Contains(id)` plus later `Remove(id)` alone can still race; the authority should guarantee the check, ID transition and state change as one coherent transaction and test exception paths. Never edit frozen original SHA. **B01-40** owns G0 physical ZIP/GUID/asmdef/migration/safe rollback; **B01-60/61** independently execute native Unity6000.3.14f1 Physics and all aliases; **B01-00** alone controls future #79 inbox. **G1 native NOT_RUN, G2 HOLD, CODEX ACK NONE, NOT INTEGRATED.**

Existing #494 versioning caveat: inherited W09 R1 source packager rejects the W07 blob replacement. Treat as expected versioned dependency-compatibility HOLD needing explicit migration; do not weaken frozen W09 blob pins or mark all CI green.

**Scope/rollback:** new QA-only `Tools/B01/B01-62/P17W11Atomicity494/**` and its one dedicated workflow, no `Assets/**`, producer or Codex edits, no merges. Close Draft/drop QA branch to roll back; remove only scratch `.stage` and `.output` in isolated test worktree (never original producer). Physical original ZIP is NOT built or claimed here; receiver B01-40 performs G0 separately.
