# B01-23 / PASS 17 / W28 — physical clearance native regression R1

**Parent:** Codex W27 #824 @ e03d36f2b86f8c73a067e574b67237a8921f622a.
**Role:** B01-23 physical construction, not canonical weapon owner. **Unity:** 6000.3.14f1.

## Production code under test
Real existing R3VehicleAssembly.ClearProjectilePath overload, unmodified. Four new Unity Test Runner [UnityTest] methods execute actual SphereCast/Collider queries and genuine Rigidbody movement in an empty scene using EnterPlayMode, NOT synthetic collision code.

1. Owned moving Rigidbody crosses a firing line; shot clearance changes CLEAR to BLOCKED to CLEAR.
2. Yaw rotation of a real muzzle changes barrel and forward corridor; original corridor blocker must stop blocking, new blocker must block.
3. Attached part blocks owner weapon; after physical hierarchy detach it becomes external world cover, not owner armor.
4. Two guns: one gun's collider blocks a different rear gun but its own forward lane remains clear.

## Explicit limitations and gates
- Source authored; actual Unity compiled/tested: NOT_RUN; Windows Player: NOT_RUN; human gameplay: NOT_RUN.
- B01-40 independent source/GUID G0: PENDING; B01-60 native G1: NOT_RUN; Codex G2 integration: HOLD.
- The detached cover scenario checks only owner-clearance classification, NOT projectile-to-debris collision/damage; do not overclaim it.
- These tests do not grant ammo, heat, cooldown, EMP, rotary saw or garage zone PASS. Existing W26/W27 suite remains unchanged.
- No Save/ECON modifications. #812 V7 loaner softlock requires B01-00 policy decision. #797 Unity incompatible API fix belongs to B01-62.

## Native execution / evidence
Use an isolated checkout of exact PR head; licensed Unity 6000.3.14f1 editor. Run real EditMode Test Runner filter B0123W28PhysicalClearanceRegressions (each test enters Play Mode). Save Editor.log, NUnit XML, Unity process exit, exact Git SHA and artifact hashes. Then run W26WeaponClearanceTests, W27SpecialWeaponClearanceTests, W27BeamClearanceTests, broader group-fire suite and extracted Windows Player blocked-fire smoke. Report executed counts only.

## Rollback
Revert this isolated branch to parent e03d36f2b86f8c73a067e574b67237a8921f622a or omit this add-only test source, its .meta and this receipt. No original W27 source was modified.

## Handoff
Next B01-40/B01-61: verify source file set, GUID uniqueness, ancestor and CI eligibility. B01-60: actual native Unity gate. B01-00/Codex: receive only after independent G0 and scope ACK; no merge/release.