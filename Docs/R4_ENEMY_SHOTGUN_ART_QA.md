# R4 enemy Shotgun art QA

Enemies using the existing Shotgun package now use the supplied R4 Shotgun appearance. The package binds to its own visible muzzle before firing. Switching from Basic/Heavy disables the old art; when the Shotgun asset is unavailable, the original renderer and muzzle remain usable. The same physical HP, tier, ammunition, pellet, damage and energy systems remain authoritative.

- Native red test at `7e13182ec0143c709b6d93d6890cf31aec2a7926`: 0/1 because the shotgun model was missing. Evidence `Artifacts/Local/RC2-R4-EnemyShotgun-Red-20261107T175022Z/`.
- Unity 6000.3.14f1 targeted EditMode at `0567a06086b8d8b71e28c17bd261e50b32e54c57`: **14/14 PASS**, exit 0. Includes actual eight-pellet tier damage, energy, durability, salvage, visual muzzle, switching and missing-model fallback, plus primary/elite gun, wheel and R5 regression. Evidence `Artifacts/Local/RC2-R4-EnemyShotgun-Final-0567a060-20261107T175436Z/`.
- Windows x64 build at that SHA: **PASS**, exit 0. Evidence `Artifacts/Local/R4-EnemyShotgun-Build-0567a060/`.
- Extracted Windows Player batch API QA: **PASS**, exit 0, including enemy wheels, standard/elite models and physical hits, swapping to the visible Shotgun muzzle and Shotgun pellets hitting a target. API-driven, not a human playthrough. Evidence `C:/Users/kubap/Desktop/Prototypy/R4-enemy-shotgun-0567a060-player-qa/`.
- ZIP `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_R4_EnemyShotgunArt_0567a060_Windows_x64.zip`, 43,542,346 bytes, SHA-256 `c4b87015670a94ea18d60cf195c8706a726c0a9a93e30091bd2038ffb476160b`. EXE matched build EXE after extraction, SHA-256 `17ad8b27359b94eac8d2f44a0052d8789dc3ff5328f44ccb8c2740be4a375eb8`.

NOT RUN: full regression, human visual/game-feel review and interactive shutdown on this build. A previous automated GUI exit crashed with `0xC000041D`; batch exit 0 does not establish it fixed. Four raw FBX-axis tests remain failing. Enemy Railgun, Rocket, Beam, Saw, EMP and other device appearances, and new shaped blocks, remain pending.
