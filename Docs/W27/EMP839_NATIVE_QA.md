# W27 / W28 EMP #839 native QA

Status: isolated QA only, not a package acceptance or game integration. Exact product source #839 `fea000fe0a706df2b52b6f295bdb3135ba1f9384`. Windows Player QA harness commit `0eb1bf164f8d201ed8b34e05e10a2663446b4818` adds only `Assets/R1/W27EmpPlayerQA.cs` and its .meta on a separate branch, guarded by `B01_UNIFIED_RC2_ACCEPTANCE` and requiring explicit `-batchmode -w27-emp-player-qa <output-directory>`. Producer source unchanged.

Unity 6000.3.14f1 Windows x64 results:

- Existing real R3/device integration `RRLW6EmpPhysicalTests` EditMode **12/12 PASS**, exit 0; original WPN006 `EmpEmitterPhysicsTests` PlayMode **22/22 PASS**, exit 0. XML/logs/exits in local `Artifacts/Local/W27/Emp839-Native/`, prior to the QA-only harness commit.
- Exact product + harness Unity compile and Windows Player build: **PASS**, exit 0 for each, local logs/exits `Artifacts/Local/W27/Emp839-PlayerBuild/`.
- Actual Windows Player, hidden batch mode: **PASS**, exit 0; actual R3 light chassis and real mounted EMP device, direct API commands. The temporarily detached gun's direct pulse was denied and preserved 40 units of energy and zero cooldown; after reattachment, a successful pulse spent 7 energy and disrupted an external primitive collider with a real EmpReceiver. JSON and Player.log: `Artifacts/Local/W27/Emp839-PlayerSmoke/`.

The Player target is a simple test collider, not a full enemy; this is not human gameplay, a manual screen/controls test, performance testing, an exhaustive radial-wall test or formal acceptance. Full W27 regression ran concurrently in another worktree; the extra Editor and Player were set below normal priority. The candidate remains separate pending required independent G0 and gameplay integration review. No merge to main/develop/release.
