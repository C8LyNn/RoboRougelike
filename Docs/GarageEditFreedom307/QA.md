# Garage Edit Freedom — QA, Issue #307

Base PR #306: `3ec920a825a72c5786f2a0dfde53bed59a2f1182`. Unity 6000.3.14f1, Windows x64. Compiled Player/gameplay source: `6db077254db0b632f06cf70de3e31466b65d5153`. Final EditMode test source: `a66a65bbff949e3a9054a2ec2e18464cb076c78f`; the only difference from build source is scene isolation in `Assets/Tests/Editor/PerWeaponReticleTests.cs`. Documentation-only commits follow.

Editing permits removal of every gun and selected-drive part, while retaining core protection, connected structure, owned part IDs/HP and inventory capacity. A live part cannot be removed into a full inventory. The garage can rebuild the existing actual vehicle with no gun, shows the reason it cannot deploy, and permits returning to map editing. `CanDeploy` and the existing activity entry gates remain in place; disabled or destroyed required equipment does not count. No main/develop merge and no swarm-owned package modifications.

## Results

| Scope | Result and limitations |
| --- | --- |
| Unity compilation and Windows x64 build | PASS, exit 0 on build source |
| Targeted EditMode after test-scene isolation | PASS 56/56, exit 0 on final test source; includes UnityTests entering actual Play Mode, real garage, model and input tests |
| Targeted native PlayMode | PASS 52/52, exit 0 on build source; camera, shotgun and EMP regression assemblies |
| Last gun + all wheels, no phantom shot, real garage rebuild | PASS in actual Unity Play Mode and unpacked Windows Player batch automation |
| One missing / damaged gun; wheels on wrong side or no wheels | PASS, deployment blocked with model reason; actual mission and range entry gates tested |
| Remounting same owned IDs | PASS, all 12 wheels and gun restored without spending scrap; actual Windows combat starts after restoration |
| Wheels → Omni → wheels | PASS through actual Unity garage and physical placement; tracks and hover transitions covered at model level only |
| Core and structural bridge | PASS existing model and real garage regression; full inventory prevents removal of a live part |
| Weapon groups and reticles | PASS targeted regression; no weapon/camera production implementation changed |
| Map editor | PASS Unity integration and unpacked Player batch: garage, persistence, R6 drive, finish and return; incomplete vehicle can return to editor while test drive is blocked |
| Interactive mouse/GUI, readability and human playtest | NOT RUN: owner requested no control of their computer; confirm manually |

Baseline reproduction: 2/2 tests failed as expected because removal was rejected. The first expanded regression run at `4c4165f21adae57b1b21823e4125a0758c858792` had 44/45 passing; the chosen Omni mounting cell was rejected by the existing physical placement gate. The test was changed to a previously established valid physical mounting point; its 10/10 retest passed. A later combined run at the build source had 53/56 passing: three reticle physics tests observed unrelated colliders left in the editor scene by earlier integration tests. The reticle suite separately passed 7/7; explicitly creating an empty test scene, without changing its assertions or production camera, produced 56/56 on the combined retest. Initial failure XML and isolated retest evidence are retained alongside the final XML.

Original Unity logs and receipts: `Artifacts/Local/FM270-GARAGE307-RC-20261027T203644Z/` and `Artifacts/Local/FM270-GARAGE307-RETEST-20261027T204114Z/`. Player logs/reports: `Artifacts/Local/GARAGE307-PLAYER/` and `Artifacts/Local/GARAGE307-MAP-PLAYER/`. Both Player processes exited 0. These tests were API-driven batch tests and do not establish visual or manual GUI acceptance.

## Build and manual test

[Windows x64 ZIP](https://drive.google.com/file/d/1z9dakBOGXSJ5BzXLYYRo9KvQWZ9Q9za4/view?usp=drivesdk): 38,648,378 bytes; SHA-256 `0f9a08ac4896d26c46da8612ea0f2e3f182fb8768f14d0ad98728e28923f5ce8`. CRC and SHA-256 of all 212 extracted files matched the original Player. Extract the whole ZIP and run `RoboRougelike.exe`.

1. Open **Garaż i poligon testowy**, choose **Zdejmij**, and remove the last gun and all wheels. Continue editing; the warning should say why an activity cannot start.
2. Use **Magazyn** to remount the same parts at valid contacts; verify one healthy wheel per side and a functioning gun. Start the test range.
3. Remove wheels again, mount the desired new drive, select it under **Napęd**, and test; check LMB/PPM, camera and map editor.
