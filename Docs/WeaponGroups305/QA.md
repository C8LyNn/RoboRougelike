# Weapon Groups R2 — Issue #305

Base: PR #304 `7249f8459c0313827472f31867ebbe62030339e6`, confirmed open Draft with clean worktree and previous QA/build published. No changes to the active map-editor branch, main, develop, or swarm-owned packages. Unity 6000.3.14f1. Windows build and final test source: `a0c50dfb7e25a61c0e55e55b94dea24e8fe68f30`. Later documentation commits do not change game assets.

A and B map the existing canonical `R4WeaponKind` types; every functioning mounted weapon of a type uses the same group. Garage swap moves a single type to B or swaps two types. Group assignment survives `DeepCopy`, keeps the slot of a destroyed plan for repair, and updates after removal or replacement. LMB and RMB can fire together. Existing weapon bindings keep their per-part resources and cooldowns. Continuous weapons stop when inputs are disabled; releasing the held button is required after resuming. The existing camera remains mouse-controlled in combat and garage/editor camera behavior is unchanged. The HUD shows both types and working counts; numbered markers remain per barrel, with spread labels for converged guns.

## Actual results

| Check | Result and scope |
|---|---|
| Compile / Windows x64 build | PASS, process exit 0 at source `a0c50df...` |
| Targeted EditMode | PASS 29/29, exit 0; includes UnityTest cases entering actual Play Mode, real scene and virtual Input System events |
| Targeted PlayMode | PASS 52/52, exit 0; camera world collision, shotgun and EMP package tests |
| Full PlayMode | PASS 474/474, exit 0 at earlier `885b999b9bc546525dfdc9cbd6d8c5b0818bf6e2`; later gameplay changes are marker label layout and text hints, plus tests |
| Broader EditMode | PASS 47/47, exit 0 at `2aa21d9d22756361273418c27236c51a79c16524`, including complete R4 combat and EMP integration tests |
| Two triggers and pause | PASS, real scene with virtual mouse events: RMB, simultaneous LMB/RMB, held input suppressed after pause until release |
| Physical projectiles and damage | PASS, both groups fired actual projectiles and reduced target HP in Unity Play Mode |
| Twelve guns | PASS, six of each type fired exactly 12 physical projectiles through group API; per-gun cooldown prevented duplicate shots; 12 marker label offsets are distinct in a numerical test |
| Damage and EMP | PASS, destroyed or EMP-disrupted weapon cannot fire in tested group; existing EMP physical integration suite passed on `2aa21d9d` |
| One type moved to B | PASS assignment/copy test; separate interactive B-only fire test NOT RUN |
| Detach/remove final gun | PASS model assignment removal; separate live-input final-detach test NOT RUN |
| Weapon inventory, recovery, type limit | PASS targeted weapon-type regression; no existing disk save/load of full vehicle was claimed |
| Camera, map editor, test drive | PASS targeted reticle and map editor integration; Windows Player batch API-driven map/garage/drive scenario exit 0 |
| Interactive GUI, human feel, actual mouse, controller, 720p/1080p and screen-edge visibility | NOT RUN: user's no-PC-control constraint; requires manual Windows playtest |

Unity XML and process receipts are in `Evidence/`. Original Editor logs remain under `Artifacts/Local/FM270-WGROUP-RC-20261027T202305Z/` and the unpacked Windows `Player.log` under `Artifacts/Local/WEAPONGROUPS-RC-PLAYER/`. Earlier full PlayMode XML is retained in `Evidence/PlayMode-previous-full.xml`. The Windows Player test is automated, API-driven and does not demonstrate manual gameplay or rendered pixels.

## Build

[Windows ZIP](https://drive.google.com/file/d/1UNkwIyX-tZajJE5ID5Rtq4sceA7QopdK/view?usp=drivesdk), 38,646,446 bytes, SHA-256 `02b55f68c2f4294b21d22ed7c29b42133bf3a6c35a72ac7db1ccaf1711c0dc78`. The archive passed CRC and all 212 extracted files matched the build by SHA-256. Extract the entire archive and start `RoboRougelike.exe`.

Manual check: open garage, add a second type, use **Zamień A / B**; enter test range, try each mouse button and both together; pause while holding, resume and confirm neither fires until released. Test 12 guns, aiming, edge markers, garage and map editor. No merge performed.
