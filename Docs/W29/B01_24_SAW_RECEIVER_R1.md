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
