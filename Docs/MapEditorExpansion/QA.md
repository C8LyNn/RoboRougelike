# Map editor: preview, floor, scaling and additional objects

Base: PR #303 at `c89744d5ab43f4a795abe62527d94439be573693`. Source used by Unity tests and Windows build: `eb87d88fad30bfb52b79f0c82cd0472a60c5af58`. Unity 6000.3.14f1; no changes to combat, vehicle physics or the player camera.

The map editor now displays the actual element geometry as a translucent green/red preview using the garage's preview materials. Red means occupied, outside the grid or an individual floor tile when the global floor is enabled. The full 101 by 101-cell floor is a single static collider and can be toggled and undone. The existing tiled floor data is retained while the full floor is enabled. X/Y/Z multipliers are independently adjustable from 1 to 4; rotation and occupied cells are validated. Zaznacz selects an existing item for rescaling and rotation and shows the garage selection color. Platform, pillar, low barrier and steps extend the original six elements. Version 2 JSON stores the floor flag and scales; R1 version 1 JSON loads with unit scales.

## Evidence

| Test | Result |
| --- | --- |
| Unity compilation and Windows x64 build | PASS, process 0 |
| Targeted EditMode | PASS 61/61, process 0 |
| Targeted PlayMode | PASS 15/15, process 0 |
| Model/geometry | PASS: legacy JSON, scaled occupancy and rotation, delete/undo, atomic resize, preview materials/geometry/no colliders, global floor collision and new object colliders |
| Actual game scene | PASS, API-driven: editor, select and resize, global floor toggle, garage, map test, finish trigger, return |
| Windows Player | PASS, process 0, batch/nographics: preview, global floor, new objects, scaled ramp, JSON, actual R6 contact and ramp traversal, finish and return; `windows-player-qa.json` |
| Interactive GUI, visual quality and human mouse input | NOT RUN: user requested no control of PC or mouse |

The initial Windows QA run failed because its lookup saw an inactive ghost pending deferred destruction; the QA lookup was corrected to check the active object and the retest passed. The render appearance and interaction still need a human playtest; batch QA verifies actual objects/material assignments, not pixels or mouse movement.

[Windows build](https://drive.google.com/file/d/1BM9VnfvvOwQQMK6xTbhkNkudoQxwlPOD/view?usp=drivesdk): ZIP 38,629,015 bytes, SHA-256 `2a8c96c54174e33e36c8dc9fd88a75ff09ead8993bc9d29b092bd409ceebbe34`. CRC and all 212 extracted game files were checked. Extract all files before starting the EXE. Original native logs are in `Artifacts/Local/FM270-MAPEXPANSION-RC-20261027T195638Z` and Windows evidence in `Artifacts/Local/MAPEXPANSION-WINDOWS-20261027T195723Z`.
