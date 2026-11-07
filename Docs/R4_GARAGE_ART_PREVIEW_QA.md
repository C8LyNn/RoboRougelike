# R4 garage preview QA

The garage mount ghost now uses the existing player-vehicle R4 weapon/wheel visuals and their axis adapters while retaining the existing nonphysical placeholder geometry and garage placement rules. It affects only an actively selected mount ghost. Missing plans are unchanged. Automatic retains its primitive preview because no Automatic model was supplied. Other movement-device previews remain unchanged.

- Red test at `421f63906b7165244b63f3956789fb439094ad8b`: 0/1, Heavy ghost did not contain the supplied model. `Artifacts/Local/RC2-R4-GarageArt-Red-20261107T170151Z/EditMode.xml`.
- Native Unity 6000.3.14f1 targeted EditMode at `b5c6bc6914285e8febeaa4a6e47fe31929745dd5`: 4/4 PASS, exit 0. Covers supplied Heavy and wheel ghost, disabled colliders, Automatic fallback, direct mounting, Heavy ghost geometry and missing-plan repair. `Artifacts/Local/RC2-R4-GarageArt-Green-20261107T170333Z/EditMode.xml`.
- Follow-up real Unity test at `22e4980b133b142dc1b24484521541e12bd80c07`: 1/1 PASS, exit 0. Heavy art position, orientation and scale matched the ghost on mounting. `Artifacts/Local/RC2-R4-GarageArt-Alignment-20261107T170427Z/EditMode.xml`.
- Windows x64 build at `e24b158908576aa931d7b98845dca01abc855ee8`: Unity build exit 0. `Artifacts/Local/R4-GarageArt-Build-e24b1589/Build.log` and `Build-process.json`.
- ZIP `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_R4_GaragePreview_e24b1589_Windows_x64.zip`, 43,537,303 bytes, SHA-256 `42cad4ebf12925144b20e45886ae27d37b0904409ba8d85e87dcfdc5777d23ff`. Extracted EXE matches the build EXE SHA-256 `17ad8b27359b94eac8d2f44a0052d8789dc3ff5328f44ccb8c2740be4a375eb8`.
- Actual extracted Player GUI smoke: reached menu and garage, removed a wheel into the inventory, selected it and observed its ghost. This is a short smoke test, not a full run or visual review of all models.
- After UI automation, exit from the pause menu returned **-1073740771 / 0xC000041D** and Player.log reported a UnityPlayer crash after Input System shutdown. Evidence: `C:/Users/kubap/Desktop/Prototypy/R4-garage-preview-e24b1589-player.log` and `R4-garage-preview-e24b1589-player-process.json`. Cause remains unknown; the window disappearing is not a clean-exit PASS.

Four tests of the original raw FBX axes still fail as described in `R4_NATIVE_VERIFICATION_20261107.md`. New block shapes, source sockets and collider stages remain experimental and are not mounted by the game. Full regression, manual player assessment, all movement-device ghosts and clean shutdown are not verified by this change.
