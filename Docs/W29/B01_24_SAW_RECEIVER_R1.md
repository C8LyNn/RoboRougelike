# B01-24 W29 Saw regression receiver (source candidate)

Parent is CODEX W28 #828 @ `887fd18bf0a518ee24776c660f5efa6e3a0491d8`.
Independent actual Unity QA #848 found #828 EditMode 2/4 and original WPN005 PlayMode 19/20. Old player and enemy saws physically overlap owner armor; their zero-RPM is correct fail-closed behavior, not a reason to bypass own geometry.

## Functional repairs
1. The WPN005 broadphase overflow in `IntersectsOtherOwnedPart(out bool)` now also sets `OverflowedLastTick=true`. It still stops the motor before heat, energy or damage; it does not partially process contacts. Original WPN005 overflow test is the regression anchor.
2. The existing player combat test now uses a real connected exterior saw mounting cell (0,1,4) rather than the central inside-hull default (0,2,0). Production garage/player mounting rules are not changed or bypassed. This fixture correction must be validated by Unity against the existing HP and attack test.
3. Existing enemy `RRLSawBinding` moves the original yaw/pitch-owned saw muzzle outside the radial bound of the *active enemy chassis* at initial equip, including the physical blade radius and margin. This uses existing colliders, not artificial hit exemptions; motor guard still runs every physics step and blocks a later moving obstruction. Enemy `preferredDistance` follows the resulting world blade reach rather than a fixed hull-center distance.

## Gates and limitations
- This is source code on a separate receiver branch, **not** production integration / accepted gameplay.
- Native Unity 6000.3.14f1 EditMode, PlayMode, Windows Player, original physics/HP/RPM/cooling, visuals and performance are NOT_RUN here; require B01-60/QA original logs and runnable artifacts before adoption.
- Risks requiring native acceptance: enemy saw may extend visibly far beyond the body; real rigidbody collisions/aiming and default camera must be observed; garage spatial preview does not yet enforce saw sweep. Do not merge on source check alone.
- No Save, ECON, EMP, main/develop or frozen producer source mutation.

## Mandatory native regression
Run exact-HEAD original `RRLW6SawPhysicalTests` (4 cases), original `RotarySawPhysicsTests` (20 cases), #848 blocked/resume QA, and Windows extracted Player API-controlled scenario. Separately inspect heat/energy/RPM and own-armour safety; log Editor.log, NUnit XML, Player.log. No QA PASS is claimed by this document.

**W29 receiver correction:** Original `Assets/scene 1.unity` serialized YAML proves `Enemy_Muzzle` is directly parented under `Enemy_Yaw` (IDs 695195063→2114861277). The original receiver helper wrongly assumed an intermediate pitch transform and would silently skip external placement. R1.1 accepts both direct and pitch-mediated yaw parents; actual Unity test remains NOT_RUN.

## W30 additional authored Unity regressions (NOT_RUN)
- `OwnerSafetyOverflowPreservesEnergyHeatAndResumesDamage` adds a real, enabled 20-unit energy reservoir, 10 energy/s, and 6 live BoxColliders against capacity 4; verifies fail-closed RPM/heat/damage/energy and recovery after physically disabling the blockers.
- `WastelandEnemySawPhysicalDiscClearsOwnHullAcrossYaw` uses the existing Wasteland enemy prefab, `Enemy_Muzzle` direct `Enemy_Yaw` parent, and real owned collider geometry at yaw 0/90/180/270. No AI or weapon source bypass is introduced; failing this native case means #861 remains HOLD.
- These two new UnityTest methods are **AUTHORED ONLY**; source readback is not a Unity test result. Re-run 4+1 W6 EditMode and 20+1 WPN005 PlayMode tests on exact final SHA, plus #848 and extracted Player.

## W31 fixture correction — owned collider requirement
The W30 overflow-energy fixture was formerly saturating the broadphase with six free-standing prop BoxColliders. This checked fail-closed but did **not** test six *owned vehicle parts*. W31 updates only this existing native UnityTest source: each BoxCollider is now a real child of `owner.transform` and must resolve `attachedRigidbody == body`, before exercising original 4-hit saturation, 0 energy/heat/RPM/damage, then resumed consumption/damage after removing all six. Native Unity execution remains NOT_RUN; no gameplay/runtime code or protected branch changes.
