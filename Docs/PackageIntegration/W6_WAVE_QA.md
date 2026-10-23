# W6 wave release QA

**Decision: READY for gameplay RC, with the unresolved Unity/UI Automation shutdown limitation below.** All eight W6 mechanics run in the game. W7–W9 remain separate work. No main/develop/W5 merge.

Frozen W5: `2154b4341dd6b6fefb135187dc07b4b12d44f908`. Full mechanical regression source: `e9ba555e6fab1798c679fe90fa4a8d0e2ad5c8de`. Six-stage native campaign/UI checkpoint: `f3af07c331f587c157f20da77cc723e9abdc14e0`. RC source including the targeted GUI reload correction: `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`. Drafts #21–28 remain stacked. Earlier feature checkpoints require #28's shared mass/lifecycle/salvage/input corrections in the tested aggregate. Unity6000.3.14f1 and package versions are unchanged; no old GUID/scene/prefab was replaced.

## Verification

| Gate | Result | Local evidence under Artifacts/Local |
|---|---|---|
| Original kit verifier | 18/18 PASS; exact requested outer SHA256 | Source verification record |
| Initial full EditMode | 750/755; five failures retained, corrected below | 20261023T160904Z-9e90709ff77347beb6a8d57f113f75f4 |
| Affected campaign/garage/W6 regression | 55/55 PASS, native0 | W6-regression-fixes-45af3a535e0643b2beefb28a039d3b62 |
| Expired gear resources/identity/paid repair/range | 3/3 PASS, native0 | W6-expired-resources-green-cd576b69b6554e18b8b82d84dae5e662 |
| Fresh full EditMode | 757/757 PASS, native0 | 20261023T165120Z-17d174630cbe4f3a8d43b151257c2886 |
| Fresh full PlayMode | 333/333 PASS, native0 | Same full-wave run |
| Beam + garage regression after native camera finding | 34/34 PASS, native0 | W6-garage-beam-green-66a48cd7d06e414d96858b686a19f6bb |
| Accessible paused training-fire control | 1/1 PASS, native0 | W6-range-pause-control-9a3096a8f570431bb8281182e63ee959 |
| GUI reload + all W6 targets | 34/34 PASS,native0 | W6-reload-green-regression-6c56b9cc9ee24fe0927766f94af789a3 |
| RC2 retail/QA Windows x64 build | Both Succeeded,native0,0 errors,4 existing warnings each; Unity totals103060840/103108376 bytes | 20261023T190431Z-2d55c129300345eeab4f2ecf81a73fec |
| Python CI helpers | 16/16 PASS, workspace-local temporary folder | W6-python-unit.log |
| Windows API/physics, every W6 device | 47/47 PASS, errors[], native0 | W6-RC2-Native-API-8f51ac32161843fbbf434a29f66b89e2 |
| Windows 1080p campaign | 6/6 victories, six rewards, final screen and restart; errors[] | W6-Native-1080-33f36c55b86c4e0b87ff7fec8023c1ea |
| Windows 720p | Garage/XYZ/module pages plus one real two-enemy victory; errors[] | W6-Native-720-Combat-95f6600df6874944ad29f620dcb97da9; earlier 720-Fixed session |
| Native extracted retail T after correction | Live screenshots show11→loading→12; operator-recorded observations, no API mutation | W6-RC2-Extracted-Retail-ad3d875ca63b4ac98cf59a92c7584089 |
| Retail ZIP | Complete dependencies, CRC,196 runtime files, every extracted file SHA256 matches; actual extracted EXE launched | release-package.json and extracted-retail session |

These counts are distinct runs and are not summed as unique coverage. Emp50 comprises35 original EMP Edit cases,10 integration cases and5 incidental Empty matches. Full757/333 predates the bounded garage/pause/GUI input fixes; the affected targets and subsequent Windows builds cover those changes. No redundant 25-minute full EditMode repeat was performed for these input/view corrections.

## Native gameplay and its limits

Menus, route selection, rewards, module purchase/mount/removal, paid repairs, final reward and restart were operated through native screenshots and mouse/keyboard UI. Combat used the existing QA pilot's ordinary virtual aim/fire/driving inputs, real projectiles, active enemy AI and physical damage. It did not invoke victory, enemy HP, reward or currency shortcuts. This is not a claim of a six-fight human/manual driving run. Separate47-check Windows fixtures use temporary actors and explicit APIs for lifecycle/damage checks; they are not manual play.

Heavy, seed2026: Pierwszy złom → Otwarty konwój → Ogień krzyżowy → Elita:Bastion → Łowcy reliktów → Oblężnik. Result15 enemies defeated, five recovered parts, six bonuses,649 final scrap,145 surviving physical parts. Paid repairs/rebuilds used earned currency. An expired0HP Shotgun retained11/12 salvos through45-scrap rebuilding and mounting;0HP enemy shield retained0charge through60-scrap rebuilding, then recharged in real combat. Mounted hull/reload/core/damage bonuses worked (core180→216→324HP). One wheel was lost in stage5 and restored by the80-scrap pre-finale repair. Menu restart cleared the run.

The sandbox checked all eight new devices across720p/1080p, including four-cell shields/PDS, XYZ rotation, stacked contact ports, EMP removal/reinstallation without duplicate charge and a test-copy return preserving the original workshop. Native range shots consumed Rocket10→9 and EMP cooldown affected three real enemy modules. Manual saw contact is not claimed; actual native contact damage is proved by the separate API/physics fixture. T raw-input and GUI compatibility are reported separately; the fixture proves resource retention and no reload on destroyed/paused/unowned devices.

## Performance

Windows1080p, Direct3D12, RTX5080, Ryzen9 9950X,60FPS cap: six combat samples totaling244.590s; maximum per-stage p95=16.671ms, p99=16.748ms, largest sampled frame17.146ms. Peak269 vehicle parts, four enemies,10 simultaneous debris bodies,184.94MiB allocated Unity memory. Longest measured state transition66.41ms.720p two-enemy sample14.284s: p95=16.670ms,p99=16.699ms,max16.853ms. These are samples on this computer at a frame cap, not uncapped or minimum-spec performance guarantees. [Machine-readable diagnostics](../Evidence/W6/native-1080-diagnostics.json).

## Corrections and unresolved issues

- Expired legacy/new weapon views no longer throw or erase ammo/device state; canonical R3 references retain actual shield/PDS loot independently of disposable Unity objects.
- Dynamic defense parts and released subtrees contribute mass once. EMP preserves physical attachment, HP and momentum, and cannot turn a temporary disruption into defeat or resurrect a wreck.
- Beam's disabled world-space LineRenderer bounds no longer move the paused garage camera away from the physical vehicle. Short weapon labels fit720p/1080p.
- Training-fire control is reachable in the Esc pause menu, outside captured gameplay mouse input.
- GUI/Unicode T events now reach the same guarded reload operation as normal InputSystem T. Initial automated T never reached InputSystem; the previous input fixture also needed an explicit Editor buffer pump. No raw input backend, project setting or magazine balance changed.
- The first full run's two obsolete defense loot counts were updated. The reward-offer assertion passed unchanged in isolation, the affected class and the clean full run; no reward logic was changed.
- **C0000005 remains unresolved.** Native sessions connected to the Windows automation client reproducibly exit−1073741819 after Alt+F4, including the completed1080p run. No in-game exception or transition hang occurred. No-Sky normal-scene Application.Quit and native device fixture exited0. Together with the existing W5 empty-scene/accessibility diagnosis this supports an automation/Unity accessibility-lifetime classification, not proof of a game-code fix. Earlier human Alt+F4 success was W5/R6/W2; a fresh human W6 Alt+F4 result is not claimed.
- GitHub Unity CI still needs its activation secret; the local licensed Editor supplies the evidence here.

## Windows candidate

`C:\Users\kubap\Desktop\Prototypy\Releases\RoboRoguelike_W6_RC2_Windows_x64.zip`

Size **38,398,940 bytes**. SHA256 `edfeb51c8e2ab726a1beec0ad9b2127fde1377e5857984be92fa4b02608d4518`. Source `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`. [Complete packaging/extraction evidence](../Evidence/W6/release-package.json). The earlier RC1 remains preserved as a superseded input-compatibility checkpoint, not the recommended candidate. No GitHub release or shared-branch merge was performed.
