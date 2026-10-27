# Weapon types — stage 1

Base: PR #301, `658d812dcd342523f25ed7d06b6443a09cacc961` (includes #300 camera and prior integration). Gameplay/test source: `5683d5c135b25ff2711e1d6e3d7a92c60919da2d`. Engine: Unity 6000.3.14f1. No camera, reticle or input code changed.

## Rule

The mounted model may contain up to two distinct `R4WeaponKind` values and no arbitrary gun-count limit. The existing spatial, mounting-contact, 180-cell and scrap rules remain in force. Destroyed plans count as mounted and reserve their type until removed; the player can store and repair any recovered type, but cannot mount a third type. Legacy/corrupt three-type designs cannot deploy or repair guns, including emergency rebuild; removing the excess part permits recovery. Inventory is not treated as installed.

## Actual native results

| Phase | Result | Evidence |
| --- | --- | --- |
| Baseline red | 3/8 pass, 5 fail as expected: gun 5 rejected, third type accepted, salvage and loaded design bypass | `Evidence/red-EditMode.xml` |
| Earlier physical fixture | FAIL in EditMode because the actual vehicle geometry uses runtime Destroy; fixture was moved to a blank PlayMode scene, without changing production geometry or assertions | `Evidence/physical-edit-failure.xml` |
| Final compile | PASS, exit 0 | `Evidence/Compile-process.json` |
| Windows x64 build | PASS, exit 0 | `Evidence/Build-process.json` |
| Final targeted EditMode | PASS 62/62, exit 0 | `Evidence/EditMode.xml`, `Evidence/EditMode-process.json` |
| Final targeted PlayMode | PASS 15/15, exit 0 | `Evidence/PlayMode.xml`, `Evidence/PlayMode-process.json` |
| Windows Player / manual play | NOT RUN: user requested no PC or mouse control | — |

The final tests cover 1/2/4/12 guns, 12 of one type, a third type, cost atomicity, destroyed plans, repair, salvage and inventory mounting, model copying and serialized part reconstruction. The 12-gun physical test rebuilds the actual vehicle and invokes the production physics/socket placement gate for each added gun in a blank PlayMode scene. The tests exercise model reconstruction but this game has no production vehicle disk-save/load flow; a real disk save/load integration test is therefore NOT RUN and must be added with that feature. Existing input-driven R4 combat scene test was adjusted to two weapon types but not run, to honor the mouse restriction.

Known inherited HUD limitation: PR #301 labels all weapon reticles after the fourth as number 4 and the weapon status list can extend beyond its panel. This is deliberately left unchanged as requested and needs a later HUD/reticle task for vehicles with many guns. LPM/PPM weapon groups remain future work. No claim is made about full gameplay or balance.

## Windows artifact

Local ZIP: `C:/Users/kubap/Desktop/Prototypy/Releases/RoboRougelike_WeaponTypes_Stage1_20261027/RoboRougelike_WeaponTypes_Windows_x64.zip` (38,615,172 bytes), SHA-256 `3d2d805bdb1682ee59f46f052ecc883f550d13e046f1156d5482664fa1a646da`. The archive CRC and each of 212 extracted game files were checked against the Unity build. Extract all files before running the EXE. Runtime launch is NOT RUN.
