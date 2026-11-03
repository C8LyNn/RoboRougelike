# B01 W29 — Rotary Saw R2: source hotfix for native #848 regression

**Immutable R1 parent:** #828@887fd18bf0a518ee24776c660f5efa6e3a0491d8; **Codex current W27 branch was NOT touched**. New versioned child R2 only.

## Source changes that actually exist
1. WPN005 `RotarySawModule.IntersectsOtherOwnedPart` now sets `OverflowedLastTick=true` when its NEW owned-collider broadphase saturates **before the motor tick**, preserving intentional fail-closed, no RPM, no energy, no heat and no damage. W28 #828 returned early and unintentionally concealed the source of block, causing the native 19/20 PlayMode failure in `OverlapOverflowDoesNotPartiallyDamage`.
2. Add read-only `BlockingOwnColliderLastTick` (nonpersisted/diagnostic) and reset it each physics step; actual colliders in physical disc set it. No owner colliders are whitelisted/ignored. Existing W28 armor-block+clear test checks correct collider and stale-null; new real Unity [UnityTest] checks saturated owned disc query, heat/RPM/damage and observable fail-closed.

## Honest unresolved product defect from native QA #848
Two **original** native W6 real gameplay tests on exact #828 fail: standard default vehicle gun converted to saw while a complete front/upper armor row is physically intersecting its blade; enemy Wasteland saw likewise has four actual own P12_EnemyBlock contacts. These are **unsafe source fixture/mounting geometries**, not license to remove own-hull blocking. This R2 does NOT silently teleport the blade, remove armor, move part IDs, change grid0.55m, or alter authoritative current garage placement rules. Producer/Codex/B01-23 must implement actual safe mounted geometry / swept motion mounting policy, native re-test default enemy with clear physical disc, and revise only demonstrably invalid fixtures after geometry correction. They must not declare R2 full native/G0 PASS until both W6 combat positives work in valid real layout. This PR is a targeted real bugfix, not a fake closure of all #848 failures.

## Tests and gates
Native Unity6000.3.14f1 EditMode NOT_RUN at THIS new R2 SHA; PlayMode NOT_RUN; Windows Player NOT_RUN. Prior original #848 native #828 result 2/4 EditMode, 19/20 PlayMode is baseline **FAIL**, not inherited green. R2 authored physical contact/overflow assertions, not executed here. No source C# compiler in this executor. B01-40/61 independent source G0 then Codex/B01-60 physical actual engine test with Editor.log+NUnit3 XML and Player. Rollback remove this versioned child only, restore original #828; never merge main/develop/release without authority.
