# Battle camera and replacement art — 2026-11-07

Tested code: `d9ff3ac683389caedda1e5bf7424e7a0d4e309f8`, Unity 6000.3.14f1.

The separate workshop sits at Y=500. With an interpolated Rigidbody and physics
paused, `R1VehicleState.MoveTo` moved its physics position but left the visible
transform in the workshop. The camera initialized around that old position and
then followed at its normal speed limit. Before the fix, a native test measured
507.28 m between camera and body; the root transform remained at Y=500.70 while
the body was at Y=0.70. MoveTo now synchronizes both poses before camera framing.

R4 gun and movement models previously replaced only the old payload renderer,
leaving the original mounting-cube renderer and its garage outline visible.
The original cube and colliders remain the canonical physical/damage parts;
only the renderer is hidden when a valid replacement model was created. Model
damage tint/flash follows that same HP owner. Garage ghosts tint the replacement
model according to placement validity. A missing model keeps its legacy fallback;
the Automatic gun still has no delivered R4 source model.

## Results

| Check | Result |
| --- | --- |
| Initial reproductions | FAIL as expected: 507.28 m camera, stale rendered pose, visible wheel cube |
| Targeted Unity EditMode, final SHA | PASS 11/11, no skips, exit 0; includes real Play Mode fixtures. [XML](QA/camera-model-fix-20261107/EditMode.xml) |
| Other supplied weapon and movement models, final SHA | PASS 11/11, no skips, exit 0; includes ranged weapons, saw, suspension, jump, blink, gyro, grapple, tracks, hover and omni. [XML](QA/camera-model-fix-20261107/OtherModels.xml) |
| Earlier broader regression | 33/34 passed on `829570e7`; the remaining debris test passed after correcting its elapsed-simulation-time wait, 1/1 on `00309129`, then included in the final 11/11 |
| Camera first battle frame | PASS: 11.82 m from body, matching physical and rendered player position |
| Movement and weapon appearance | PASS targeted: all 10 supported movement adapters plus wheel and Basic gun hide their old shells; physical colliders remain |
| Missing model / ghost / damage | PASS targeted: fallback stays visible, invalid ghost tint, model damage tint/flash and detached model checked |
| Windows x64 | PASS build exit 0, 0 errors and 12 warnings; ZIP extracted, required runtime files present |
| Windows Player with Direct3D 11 | PASS API-driven, exit 0, camera, models, colliders, model damage and prior enemy weapons. [JSON](QA/camera-model-fix-20261107/WindowsPlayer.json) |
| Null-graphics Player fixture | FAIL: the fixture could not find a material with `_BaseColor` under `-nographics`; the same executable passed using Direct3D 11 |
| Windowed GUI automation | Garage and first encounter observed; [garage](QA/camera-model-fix-20261107/Garage.png), [battle start before zoom input](QA/camera-model-fix-20261107/Battle-start.png). This is not a complete human playthrough |
| Windowed automated exit | FAIL: window closed but Player.log and minidump report `0xC0000005` in UnityPlayer after shutdown; exit code not captured. Cause and normal manual exit remain unverified |

ZIP: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike_Kamera_Modele_d9ff3ac6_Windows_x64.zip`,
43,546,260 bytes, SHA-256
`d6ce646437a03240216c895c672e42e713aaa13daa3775cccf6d51be06c1e929`.
Extract the entire archive before running the EXE.

Full Editor/build/Player logs remain in local `Artifacts/Local/` directories
`RC2-CameraStart-Replacement-Final-d9ff3ac6-20261107T215136Z`,
`RC2-Replacement-OtherModels-d9ff3ac6-20261107T215652Z`,
`Camera-ModelFix-Build-d9ff3ac6`, `Camera-ModelFix-Player-d9ff3ac6` and
`Camera-ModelFix-GpuPlayer-d9ff3ac6`. Full regression, standalone PlayMode
suite, manual visual/gameplay acceptance and a diagnosis of the existing
automated exit crash remain outstanding. No merge to shared branches performed.
