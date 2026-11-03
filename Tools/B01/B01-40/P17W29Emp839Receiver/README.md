# B01-40 W29 — SELECTIVE INTEGRATION CANDIDATE: original EMP #839 on current playable W28 game

**This branch contains real integrated game-source changes, not another EMP prototype.** Exactly two original authoritative runtime C# source blobs from #839 plus its original PlayMode test are byte-for-byte copied from the original owner commit. The original Windows Player opt-in test + GUID from #849 are included for native operator regression. The base is *actual latest verified current playable Codex W28* `feature/w27-weapon-garage@ed8cf4bcc131688b9a93968a89683d2496586683` and includes all W27/W28 running gameplay and garage improvements at that HEAD, not a fresh project or stale W27 replay.

## Identity and source ownership — no new EMP implementation

| Item | Exact original source | Provenance |
|---|---|---|
| Current game base | #824 `ed8cf4bcc131688b9a93968a89683d2496586683` | active game preserved, separate isolated receiver branch only |
| EMP producer | #839 `fea000fe0a706df2b52b6f295bdb3135ba1f9384` | existing original `EmpEmitter.TryPulse` + original `RRLEmpBinding` direct revalidation of HP/attachment/EMP disruption |
| Original EMP PlayMode regression | same #839 | original `EmpEmitterPhysicsTests.cs`, including direct-call authority/no energy/cooldown consumption |
| Windows Player QA harness | #849 `16d338f13779ee71ac74b8549e0168dce92bfda3` | original source + meta `W27EmpPlayerQA`, activated *only* by `-w27-emp-player-qa` in an explicit test Windows Player build |

**Already reported native provenance before selective integration:** original #839 EditMode 12/12 and original EMP PlayMode 22/22 Unity 6000.3.14f1 PASS; original #849 Windows x64 Player detached emitter → denied, reattached → real EMP receiver affected, exit0. Those logs belong to **original test candidate**, NOT to this new combined W29 receiver HEAD. **Do not carry them forward as new final-G1 evidence.**

## Exact three real source changes — physically included

- `Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs` original #839: optional trusted gate checked on every direct `TryPulse`, before energy/cooldown and authoritative spherical overlap/line-of-sight evaluation.
- `Assets/WeaponIntegration/RRLEmpBinding.cs` original #839: binds current R3 `Device.Available`, real parent attachment `IsChildOf(Device.Owner)` and original EMP `RRLEmpPartState.IsBlocked`. Does not grant fake resources or transform EMP into projectile.
- `Assets/RoboRougelikeMechanics/WPN006EMP/Tests/PlayMode/EmpEmitterPhysicsTests.cs` original #839 Unity Test Framework source: blocked direct emitter preserves 40 energy and 0 cooldown, legitimate pulse consumes exactly once. Original other WPN006 radial tests remain unchanged.
- `Assets/R1/W27EmpPlayerQA.cs` + original `.meta` GUID from #849: opt-in actual Windows Player test exercises R3 vehicle, detach/reattach, original energy bank and real receiver. It is guarded by `#if B01_UNIFIED_RC2_ACCEPTANCE` and explicit command line; no auto-run or user Save effects.

**No changes** to `R3VehicleAssembly`, R3 mounting/garage, AI enemies, #828 Saw, Projectile/Beam, #826 Save, ECON V7, Asset generator, active Codex worktree or any protected branch. Owner PR #839 remains unchanged. The receiver was created with Git-tree atomic blob selection, not merge/rebase/copy of entire historical producer branch.

## Executable independent source-only G0

```bash
python Tools/B01/B01-40/P17W29Emp839Receiver/verify.py --receipt evidence/b0140-w29-emp839-original.json
```

The executable checks exact original 839/849 Git delta scope/ancestry, correct current gameplay original base with all three EMP files **unchanged** from parent of #839, exact byte identity of 3 original modified owner code/test blobs and both original #849 QA player files, original R3 enemy/player EMP dispatch preserved, radial non-projectile physics and activation-gate original production signatures. Checks no other owner path changed, new `.meta` GUID globally unique in actual current game, physically stages exact originals into NEW own disposable temp directory, runs a dirty-copy fail-closed source witness and confirms foreign player Save/empty directory untouched. Generates an immutable per-source Git blob SHA/byte receipt. CI runs original Git checks on Windows2025 and Ubuntu24.04 and compares deterministic inner manifest bytes.

This constitutes **SOURCE-SELECTED-IN-GAME** and source-G0 only; neither native Unity import nor Player run of the combined W29 branch is executed by the Python gate. Treat any source mismatch as red, stop integration/merge and restore the exact original W28 head.

## Native G1 and receipt required before actual gameplay acceptance

Only a deliberately isolated, user-authorized activated **Unity 6000.3.14f1 Windows** checkout at the **current selected receiver exact commit** can run these:
1. Complete Unity compilation and log errors/warnings; targeted existing real `RRLW6EmpPhysicalTests` EditMode, original `EmpEmitterPhysicsTests` PlayMode and full EditMode/PlayMode; player group attack and enemy EMP/interrupt. Attach Editor.log, NUnit3 XML and process exit per run.
2. Windows x64 Player build from **this exact W29 receiver SHA** with correct `B01_UNIFIED_RC2_ACCEPTANCE` define, ZIP it and hash SHA256, *extract ZIP in a fresh disposable directory*, run with `-w27-emp-player-qa <distinct-disposable-output-directory>`, inspect generated `w27-emp.json`, genuine Player.log and process exit0; denote that this is **API-controlled**, not human playtest. Ensure default player build does not enable batch QA.
3. Real in-game EMP with both mounted player and enemy, detachment mid-fight, enemy modules, real obstacles and no resource consumption when blocked; profile and screenshot as appropriate. Newly integrated QA must not assert this prior to execution.
4. On any native error, maintain this Draft as HOLD, do not merge/release, do not leave an altered user Save, and return failing exact logs to original Codex EMP owner; do not introduce a competing repair without owner approval.

**Not covered by this branch:** Saw #828 still 2/4 EditMode +19/20 PlayMode native RED, garage real sweep validator/five colors, #826 typed Save/Unity binder G1, P0 #812 loaner dead-end, new Jump Pad/Charge/VFX and final all-weapons Player. They remain separate owner gates.

## Rollback and status

Reverting this **unmerged isolated** Draft to original `ed8cf4bcc131688b9a93968a89683d2496586683` removes exactly the 3 original #839 changed blobs + 2 original opt-in #849 files + own test docs/workflow, without touching user Save or any active Codex worktree. Never revert whole historical PR or delete real gameplay files.

`IMPLEMENTED=YES_ORIGINAL_EMP839_PRODUCT_SOURCE_NOW_IN_W29_RECEIVER`;
`SOURCE_G0=CI_DEPENDENT`;
`TESTED_NATIVE_ON_EXACT_COMBINED_RECEIVER=NOT_RUN`;
`PRIOR_ORIGINAL_839_849_G1=PASS_LIMITED_SCOPE_DIFFERENT_HEAD`;
`G2_READY_FOR_GAME=HOLD`;
`INTEGRATED_IN_OWN_ISOLATED_SOURCE_BRANCH=YES`;
`INTEGRATED_IN_CANONICAL_GAME=NO`;
`RELEASED=NO`; `CODEX_ACK=NONE`.
