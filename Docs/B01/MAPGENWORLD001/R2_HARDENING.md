# MAPGENWORLD001 R2 source hardening (B01-21 / autonomous production)
Version: 1.0.1 (backward-compatible public adapter API). Branch: existing Draft PR #243, **not a second producer PR**.

## Problem A — confirmed source archive hygiene defect
B01-40's independent W12 audit [#71 comment 6401924466](https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-6401924466) verified that 1.0.0's real full inner ZIP contains generated `Tools/B01/MAPGENWORLD001/obj/*` paths. Original `package_world.py` skips only selected extensions and `__pycache__`, so .NET `obj/project.assets.json` leaks into a future SOURCE ZIP. This creates ambient-machine-dependent bytes and irrelevant installer files.

**Fix:** `gather()` excludes build/cache components `obj/bin/__pycache__` etc.; `validate_assets()` and `verify_zip()` independently FAIL-CLOSED with `GENERATED_FILE_NOT_ALLOWED` if a malicious manifest/ZIP contains them. Keep actual package-owned `.cs/.asmdef/.meta`, docs, reproducible scripts; no Assets/B01Packages.meta overwrite.

**New negative tests:** contaminate disposable copy of package tree with four compiler/cache outputs, verify byte-identical clean collection; confirm no generated entry in manifest; forbid injected generated names into a constructed file map and ZIP. No host or B01-40 source changed.

## Problem B — insufficient WORLD-space result validation
The 1.0.0 adapter checked root activation, geometry hash and finite endpoints only. A broken/host-transformed `MapResult` with mismatched seed, wrong generation version, missing checkpoints or finite points **outside the actual colliders' world Bounds** could still produce a world-activity `WorldReady` callback. A checksum is insufficient to validate callback metadata.

**Fix:** inside *our* `MapGenWorldAdapter` post-build guard, require requested seed/recipe, expected generation version, all checkpoint/spawn counts, and finite Start/Finish/Checkpoint/Spawn values inside *actual world-space* `MapResult.Bounds` (2.5 cm float tolerance only). Main physical route selection remains the untouched producer's job. Because the producer commits the newly built map before returning, a corrupt post-build result must call `MapGenBuilder.Clear()`, drop prior snapshot and emit `WorldCleared` once; it cannot honestly claim transactional preservation of the already-destroyed old scene.

**New independent fake-builder tests:** eight injected faults (outside start/finish/checkpoint/spawn, missing checkpoint, wrong seed, wrong version, NaN finish), preserving previous already authored test semantics. These run the **real Unity adapter C#** against explicitly synthetic MonoBehaviour and **fake** MapGenBuilder, **NOT native Unity**. Added real authored PlayMode regression on an offset+rotated generator host to exercise genuine world-space Bounds under Unity. Source planner geometry unchanged; protected centreline vs optional side ramp remains distinct.

## Expected QA and gates
- Execute actual source CI 31 existing policy rows, **16 adapter fake-source rows**, **12 package unit rows**, deterministic two-archive compare and SHA-256/final inner ZIP receipt. If tests fail, RED→FIX→RETEST before publication.
- Execute genuine Unity 6000.3.14f1 PlayMode 4 tests (3 original+1 rotated host) and EditMode 6, and installed RC1 MAPGEN smoke from Codex [Draft #248](https://github.com/C8LyNn/RoboRougelike/pull/248) on a *disposable* worktree. **Neither is claimed PASS by headless CI.**
- B01-40 must independently audit new full ZIP/GUID and target collision. Existing B01-40 [G0 R2](https://github.com/C8LyNn/RoboRougelike/pull/246) only certifies **old v1.0.0** exact ZIP; no inherited PASS for v1.0.1.
- Existing experimental RC1 #248 and MAPGEN producer R3.4.1 #237 remain frozen. No Codex target edits, no package install into active game, no merge/G2. Final SHA and actual Actions link supplied in new Draft PR update only after CI.
