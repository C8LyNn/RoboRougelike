# B01-23 — W29 Rotary Saw overflow diagnostic corrective source (R1)

**Parent immutable candidate:** Codex #828 @ `887fd18bf0a518ee24776c660f5efa6e3a0491d8`; QA #848 reproduced full candidate Unity EditMode 2/4 and WPN005 PlayMode 19/20. **This is a stacked isolated corrective candidate, not a producer branch overwrite or integration.**

## Concrete original production fix
The original #828 `IntersectsOtherOwnedPart()` fails closed when `Physics.OverlapSphereNonAlloc` returns at least `overlapBuffer.Length` but returned early **without** setting `OverflowedLastTick`. Because the later damage-contact query is skipped, the existing public diagnostic always stayed false. This broke the original native `RotarySawPhysicsTests.OverlapOverflowDoesNotPartiallyDamage` despite still refusing to cut.

In `Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs`, the existing saturated guard now sets `OverflowedLastTick = true` **before** returning `true`. No contact-ownership filtering, damage policy, RPM/energy/cooldown, disc radius or buffer-size rule was weakened.

## Real physical native regression added
Original `RotarySawPhysicsTests.cs` gets `OwnerOverlapOverflowFailsClosedAndRecoversWithDiagnostic`: genuine five overlapping Unity BoxColliders against a four-element nonalloc buffer, real WaitForFixedUpdate; require explicit owner obstruction diagnostic, overflow diagnostic, RPM=0, Heat=0, damage=0. Disable four owner obstacles, then another actual physics tick must clear both diagnostics, allow RPM and resume real external contact damage. Existing `OverlapOverflowDoesNotPartiallyDamage` remains untouched and should also be rerun.

**TEST SOURCES AUTHORED, NOT EXECUTED IN UNITY 6000.3.14f1.** No fabricated Editor.log, NUnit XML, Player log or Windows ZIP. Source verify: compare exact #828 → this child, check only runtime, its existing PlayMode test file and this document. Need B01-40/61 independent G0 and B01-60 actual Unity edit/play/Windows Player test, including full WPN005 and W27 integration.

## Still blocked (P0 — DO NOT INTEGRATE #828 WITH THIS PATCH ALONE)
#848 native collider diagnostics found **real** stock R3 player armor `(0,1,0..3)` and enemy `P12_EnemyBlock` intersecting the spinning blade disc. The existing correct fail-closed guard legitimately prevents damage/RPM, so ignoring hull colliders would regress self-protection. B01-23 does **not** silently reposition saw mount / change the 0.55m grid / move another agent's prefabs / bypass the authoritative garage. Requires a Codex-owned geometry/mounting correction that is consistent in physical prefab, garage preview, R3 stock and enemy rigs, plus native contact tests after correction. Until then `DEFAULT_R3_SAW_GAMEPLAY=FAIL/HOLD`.

## Gates / rollback
- IMPLEMENTED: source correction and authored physical regression
- Source identity: verify exact Git blobs and 3-file diff; independent G0 PENDING
- Native Unity compile/Edit/Play: NOT_RUN on corrective head
- Windows Player, original stock R3 Saw, Wasteland enemy: NOT_RUN on corrective head
- PRODUCT_G1: HOLD, G2: HOLD, CODEX_ACK: NONE, merge: NO, release: NO
- Rollback: omit or revert only this child PR on an isolated checkout. #828 parent is unchanged. No automatic migration of rigs or Save.

**Next owners:** Codex (stock saw physical geometry and playable integration), B01-40/61 (G0), B01-60 (native G1), B01-00 (acceptance prioritization).