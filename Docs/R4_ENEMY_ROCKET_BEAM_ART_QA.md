# R4 enemy Rocket and Beam art QA

Enemies using the existing Rocket Launcher and Beam Cutter packages now use their supplied R4 models and bind the packages to the visible muzzles. Missing source models fall back to the original renderers and muzzles. Existing physical rockets, beam raycasts, energy, HP and damage systems remain authoritative.

- Native red tests at `ef06a929ba5d134d70aa4afe52d5205494d29fec`: 0/2 because both visuals were absent. Evidence `Artifacts/Local/RC2-R4-EnemyRocketBeam-Red-20261107T180537Z/`.
- Unity 6000.3.14f1 targeted EditMode at `0068abd4db73820a78ef355a89228a2806a7c6c2`: **14/14 PASS**, exit 0. Includes actual rocket and beam damage/energy, correct muzzles and fallback when either model is unavailable, plus Railgun, Shotgun, primary/elite gun and wheel regression. The beam fixture invokes the real fixed simulation with trigger held; it is API-driven. Evidence `Artifacts/Local/RC2-R4-EnemyRocketBeam-Final-20261107T180827Z/`.
- Windows x64 build at that SHA: **PASS**, exit 0. Evidence `Artifacts/Local/R4-EnemyRocketBeam-Build-0068abd4/`.
- Extracted Windows Player batch API QA: **PASS**, exit 0. Checks actual Rocket and Beam damage, supplied models/muzzles, Railgun, Shotgun, standard/elite guns and enemy wheel detachment. Beam fixed simulation is invoked by the harness; this is not human gameplay. Evidence `C:/Users/kubap/Desktop/Prototypy/R4-enemy-rocketbeam-0068abd4-player-qa/`.
- ZIP `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_R4_EnemyRocketBeam_0068abd4_Windows_x64.zip`, 43,543,441 bytes, SHA-256 `47d4fcf5c11a7d6d41ed8adcd7792e34f4b37c5fa9d9dd17be5b52ea3148d03f`. EXE matched build EXE after extraction, SHA-256 `17ad8b27359b94eac8d2f44a0052d8789dc3ff5328f44ccb8c2740be4a375eb8`.

NOT RUN: full regression, human visual/game-feel review and interactive shutdown on this build. A previous automated GUI exit crashed with `0xC000041D`; batch exit 0 does not establish it fixed. Four raw FBX-axis tests remain failing. Enemy Saw, EMP and other device appearances, and new shaped blocks, remain pending.
