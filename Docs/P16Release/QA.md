# PASS 16 — integrated test build and status

Branch `feature/mov009-ux-309`; gameplay and build source `9ebc2df0508b5ff3cbe561d3170093994241658e`, PR #310; Unity 6000.3.14f1, Windows x64. This report and its evidence are documentation-only additions after the tested source. No merge to main/develop.

## Actual results

- PASS: Unity compilation and Windows build, each native exit 0, build log reports 0 errors and 0 warnings.
- PASS: full PlayMode 474/474, 0 skipped, native exit 0, on the exact gameplay/build SHA.
- PASS: selected EditMode 13/13, 0 skipped, native exit 0, on that SHA: atomic invalid-reward rollback; physical ramming, disabled-drive and wall cases; LD02 physics; six enemy target validity cases. Tests use actual Unity/physics and scripted controls.
- NOT_RUN: full EditMode on this SHA. The older full run on `aa7a917f` failed 15/1303; after updating stale assertions and physical input pilots, those 15 passed targeted, but this is not a full-suite PASS. See `Docs/CXFinal/QA.md`.
- PASS: extracted Windows Player in hidden batch mode, garage/combat entry, map editor/actual R6 ramp/return, and MOV009 status/force, 3/3 scripts; each native exit 0. These are automated, not a human gameplay or GUI test. No exceptions or fatal errors in those Player logs.
- PASS: all 212 files extracted from the ZIP have the same SHA-256 as staging. Drive-reported size equals local size; Drive remote hash was not exposed by the connector.
- NOT_RUN: visible UI, human gameplay, end-to-end real player playthrough, enemy dynamic ally avoidance, human long-range aiming and visual quality at different frame rates.

## Changes and remaining work

- DONE: invalid victory catalog no longer repeatedly awards currency when the victory transition fails (`a2ccd055`); reproduction involved a deliberately corrupted catalog, not an assertion of normal reachability.
- DONE: enemy targeting rejects disabled, inactive, detached and dead parts (`561c223f`). Original external B01-24 test needed an NUnit compatibility adaptation; its exact source is preserved. A policy-conditional cover test was unintentionally run and failed: an enemy fires toward an occluded target; this test did not establish whether its projectile passes through the wall. The approved cover/LOS contract and a production fix remain unresolved; see `Docs/P16AI/QA.md`.
- DONE: unarmed mobile enemies physically approach and contact the player using existing R6 forces (`16c26681`); destroyed wheels cannot provide artificial propulsion, solid walls still block. No new ramming damage rule.
- PARTIAL: LD02 perimeter and Light/Heavy straight traversal passed (`9ebc2df0`); enemy spawn envelopes, combat, obstacle/cover interactions, reward transitions and Player rendering are NOT_RUN. LD02 is not exposed in the game. See `Docs/P16LD02/QA.md`.
- NOT_STARTED: durable save/load of an entire run. Maps can be saved and vehicle state survives the current in-memory run, but closing the game does not persist a complete expedition. A safe implementation must preserve unique part IDs, ownership/condition, progress and rewards without duplication.
- PARTIAL: enemy multi-gun variants exist and are covered by older targeted tests, but a general two-independent-weapon-group AI contract and friendly-fire/ally avoidance are not fully validated. Further physics, combat and true player playthrough remain.
- Previous D01/D02, discounted rebuild, two-family limits, group memory and other integrity changes remain described in `Docs/CXFinal/QA.md`. Independent B01 receiver acceptance has not been claimed.

## Windows test package

[Download on Google Drive](https://drive.google.com/file/d/12TTBYNQ1BXZGZ9w3dTtOoYTZhl1j2Sxf/view?usp=drivesdk); 39,624,084 bytes; SHA-256 `f6fe17e6951260ad9cd9749c355dda37dec7ac1926d8aa2af971fd8489738505`. Local: `C:\Users\kubap\Desktop\Prototypy\Releases\RoboRougelike_PASS16_9ebc2df0\RoboRougelike_PASS16_Windows_x64.zip`. Extract the whole archive and start `RoboRougelike/RoboRougelike.exe`. Test garage, map-editor save/test/return, suspension, two fire groups and combat. This is a test build, not a claim that all PASS 16 work is complete.

