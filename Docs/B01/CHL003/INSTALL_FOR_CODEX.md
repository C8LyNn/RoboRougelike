## Dependency-aware C1 SHA guard / STOP

Prior source W17 SHA `f4ca318577edbdf4d003ab3e104ac7ec71d83cfe` and its archived ZIP are now HISTORICAL as soon as owner CHL source moves. Do NOT re-use old B01-40 G0; download the exact new source archive from the GH Action that PASSed on the exact current PR #285 HEAD and verify per-file SHA256 and GUID, including `.meta`. G1 native requires 19 EditMode / 14 PlayMode, real Light/Heavy 3 runs each, plus current Heavy yaw8 old 3.5m/new 3.7m CP0 test P14. No active Codex import, G2 HOLD.

## W17 integration quarantine / exact test instructions
Current R1 producer W17 additionally requires Unity Test Framework **19 Edit/14 Play** real tests, particularly P07 Light+Heavy×3 and P10–P13 traction/negative crossings, on disposable Unity6000.3.14f1; raw Editor.log, NUnit XML and actual exits plus HUD/CSV are mandatory. Stop if the recipient has conflicting CHL assets/GUIDs. The release is **NOT CODEX_READY**, **NOT OFFERED**, G2 HOLD until independently accepted. Previously recorded SHA `30c0355a62764eb37798347612564ee51f3b36e1` and its archive are historical and must NOT be installed as W17.


# CHL-003 R1 — SAFE INSTALL / ROLLBACK, QA ONLY

**Do not import into active Codex worktree. No G2/merge.** Read final pinned source SHA, downloaded physical ZIP SHA256 and manifest first. Source branch `feature/b01-20-chl003-r1-physics-course-20261027`, base main `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`, package entirely within `Assets/RoboRougelikeChallenges/CHL003/**`, plus parent `Assets/RoboRougelikeChallenges.meta`. This does not modify existing `Assets/B01Packages.meta`. In disposable Unity 6000.3.14f1 URP clone, verify all sha256/bytes and importer GUIDs and copy only authorized new Assets tree. If recipient already has `Assets/RoboRougelikeChallenges`, STOP and compare owner GUID and paths rather than overwrite. Keep paired script/scene/folder `.meta`; do not regenerate GUIDs.

Import Test Framework/NUnit as already configured, open `Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003_Demo.unity`, run actual Editor compile then Edit19/Play14 Test Runner, then manual Light+Heavy physics driving. Save Editor/Player logs, actual exits, NUnit XML and screenshots. Do not change R1/R3/R6/MAPGEN/Pack/Build/P12/physics settings or scene list. No W8 adapter yet; future integration should be explicit Codex-owned optional host session invoking Configure/Begin and listening events, never direct money/reward changes.

Rollback on disposable source checkout: delete ONLY imported `Assets/RoboRougelikeChallenges/CHL003/**` and `Assets/RoboRougelikeChallenges.meta` only if the enclosing folder was newly created by this import; restore any pre-existing folder/scene exactly by manifest/receiver receipt. Verify canonical game diffs 0 and refresh Unity AssetDatabase. No main merge, no active installation.
