# MAP EDITOR R1 — integration and QA

Base: `d44fa09c227dffca1c1f8832383bbd80430bba90`, PR #302; gameplay/test source and Windows build: `b1ba41ffdb6e9c0368910c8fc439565f03e36ec4`. Unity 6000.3.14f1. The R2 archive in #67 was not present in the available Downloads/Desktop or repository branches; MAPGEN is a different system. The editor uses the existing garage, R3 assembly, R6 wheel drive, camera and pause menu.

## Player flow

Main menu -> Edytor map. An empty draft opens with six placeable types, 4 m grid, V rotation, remove, undo/redo, WASD pan, right-drag camera rotation and wheel zoom. Enter a name and use Zapisz/Wczytaj; local JSON is in `Application.persistentDataPath/Maps`. A start on ground and a finish are required for testing. Dostosuj pojazd opens the existing garage; Wróć do edytora restores the draft. Testuj mapę creates a copy of the real vehicle, uses R6 and physics colliders, and Esc opens a pause menu with a return to the same draft and original garage vehicle. A cyan marker indicates start and a yellow marker is the finish trigger.

## Actual results

| Test | Result | Evidence |
| --- | --- | --- |
| Compilation | PASS, native exit 0 | `Evidence/Compile-process.json` |
| Windows x64 build | PASS, native exit 0 | `Evidence/Build-process.json` |
| Targeted EditMode | PASS 56/56, native exit 0 | `Evidence/EditMode.xml` |
| Targeted PlayMode | PASS 15/15, native exit 0 | `Evidence/PlayMode.xml` |
| Map document, invalid inputs, undo/redo and JSON | PASS | MapEditorDraftTests in XML |
| Actual R3/R6 wheel forces crossing generated ramp mesh | PASS | MapEditorDraftTests in XML; wheel contact and travel traced in Unity Editor.log |
| Main menu -> editor -> garage -> test -> pause -> editor | PASS, API-driven Unity scene | MapEditorIntegrationTests in XML |
| Finish trigger | PASS, real collider with a deliberately repositioned vehicle in Unity test; physically crossed in the Windows test below | XML and player report |
| Windows Player x64 | PASS, exit 0, batchmode/nographics and API-driven; actual R6 wheel contacts, ramp crossing and finish, JSON save/load, garage and editor return | `Evidence/windows-player-qa.json`, `Evidence/windows-process.json`, `Evidence/windows-player-log.txt` |
| Interactive visual/mouse/game-feel test | NOT RUN: user forbids taking PC/mouse control | Manual play required |
| Extracted ZIP files | PASS: CRC plus SHA-256 comparison of all 212 Unity files | local archive verification |

The Windows drive test uses the real player Rigidbody and R6 drive `Step`, not a substitute controller; test driving is programmatic, not proof that a person has checked mouse, UI, rendering or driving feel. The normal retail camera cursor code is unchanged outside batch mode; batch mode avoids touching the user's system cursor. Earlier model tests initially failed because EditMode yield objects were incorrectly used to count physics steps; the fixture was corrected to wait for actual fixed-time increments. A separate regression run had 3 reticle failures due to a test scene left open; cleaning the test scene produced the final 56/56 result. No failures have been relabeled as passes.

## Artifact

[Download Windows ZIP from Google Drive](https://drive.google.com/file/d/1DS7Ggc_IhMJcU7bHgZoQNTyirHRLONKz/view?usp=drivesdk). Local ZIP: `C:/Users/kubap/Desktop/Prototypy/Releases/RoboRougelike_MapEditor_R1_FINAL_20261027/RoboRougelike_MapEditor_R1_Windows_x64.zip`, 38,625,134 bytes, SHA-256 `0c58cf855e243b4aed6f7efcd31d53baba809377fbd46dd865fe938dd3c797c5`. Extract all files and start `RoboRougelike.exe`. Native Unity logs and original XML are retained locally in `Artifacts/Local/FM270-MAPEDITOR-FINAL-20261027T144506Z`; they are not represented as manual validation.

Known inherited limitation from #301/#302: weapon reticle labels above gun 4 repeat number 4. This task does not change weapon behavior. User-facing mouse interaction and image quality still require a human playtest.
