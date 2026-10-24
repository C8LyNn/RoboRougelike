# MODEL-INTEGRATION-001 — Visual override pilot (DRAFT / Unity NOT RUN)

Target Unity: 6000.3.14f1, URP 17.3.0. Base: `develop`.

## What was fixed
- Optional `RRLPartVisualCatalog` resolves a prefab by R3PartKind / weapon / left-or-right side.
- `PartVisualBinder` substitutes **only legacy child renderers** in both live vehicle and garage ghost.
- Existing logical GameObjects, Rigidbody ownership, colliders, HP, weapon muzzle, modules, grid and economy are not changed.
- Rejects prefabs containing Rigidbody or Joint. Imported colliders are disabled.
- Follows existing logical wheel/gun pivot WITHOUT inheriting non-uniform scale.
- If catalog or prefab is absent, preserves all legacy visuals (fail-open for rendering, fail-closed for physics).
- 4 EditMode tests included. **NOT RUN in Unity here.**

## How to activate
1. Open in Unity 6000.3.14f1, confirm current project compiles.
2. Install official glTFast via Unity Package Manager, `com.unity.cloud.gltfast` (version compatible with Unity 6.3).
3. **In Unity**, import W01 R2 left/right GLB and H01 V4 GLB, inspect importer warnings, materials and scale. Do not copy standalone Runtime scripts from asset ZIPs into gameplay.
4. Create ScriptableObject `RoboRougelike > Part Visual Catalog` at `Assets/Resources/RRLPartVisualCatalog.asset` (exact resource name).
5. Configure entries for `Wheel` with sides Left and Right, and `HoverEngine` (Any). Drag imported model assets to `prefab`.
6. Set `localOffset`, `localEuler`, and `localScale` per asset. NOTE: source wheel axis +X; legacy Unity wheel pivot uses local Y rotated into +X, so set rotation offset from actual imported Unity axis. Verify in play mode; do not assume values.
7. Keep `followLogicalPivot = true` for Wheel/Gun; for HoverEngine use false. Hover physics remain in MOV007, **do not** attach an extra hover controller.
8. Preview in garage and range; then run EditMode / PlayMode and Windows build. Collect screenshots and logs.
9. Only after passing actual Unity checks, create / save real prefab variants and accept the pilot.

## Known blockers
- GLB binaries are currently in external packages, not committed in this PR. Without them and the catalog asset, the original game visuals are intentionally unchanged.
- `Packages/manifest.json` and lock are intentionally NOT edited until Unity is available to resolve importer dependencies reproducibly.
- Imported GLB animation clips may require separate binding. This pilot follows logical pivots; rotor and flaps need their own visual driver in the next gate.
- Garage highlighting overrides renderer materials; check how imported PBR materials interact with previews.
- The imported model can extend beyond the original physics collider. Evaluate geometry/visibility but do not silently replace collider.
- `RRLPartVisualCatalog` entries are references to Unity assets, not external paths. Codex needs physical GLB files accessible in its checkout/workspace.

## Acceptance
- No catalog: original visuals unchanged.
- Left and right wheels resolve separate assets; no negative scaling.
- Prefab with Rigidbody or Joint is rejected; original visible.
- No new body/collider forces; no changes to existing R6/MOV physics.
- Existing garage, damage/detach, salvage, player/enemy, and weapon muzzle tests pass.
- Unity EditMode, PlayMode and Windows Player QA are required; do NOT mark PASS without actual logs.

This branch is a foundation for Codex local integration, not a completed full-asset rollout.

## Repeatable Unity setup command (new)
After copying the original GLBs to the exact paths:
- `Assets/RRLImportedModels/W01/W01_R2_Game_L.glb`
- `Assets/RRLImportedModels/W01/W01_R2_Game_R.glb`
- `Assets/RRLImportedModels/H01/H01_V4_SideDrive_Animated.glb`

Run menu: `Tools > RoboRougelike > Visual Models > Configure W01 + H01 Pilot`.
The command validates the imported prefabs BEFORE changing the catalog, preserves
unrelated catalog entries and writes Resources/RRLPartVisualCatalog.asset.
Review scale, axis and pivot on Windows Unity — the provided numbers are
starting estimates, not verified by runtime tests. Do not track Unity-generated
model .meta files until import and visual QA are satisfactory.

## Wheel spin correction
- W01's imported GLB is nested below a pose-follower anchor. The original logical collider and module remain unchanged.
- `PartVisualWheelSpin` rotates the W01 child around its +X axle using chassis forward speed in LateUpdate, no added forces.
- A dead/detached wheel stops spinning; a garage ghost has no Rigidbody and stays static.
- The visual radius baseline (0.39 m) matches the old wheel proxy and requires Unity on-screen calibration.
- Added two EditMode tests for wheel rotation and preserving nested animation transforms. No test execution claimed.

## Repeatable verified GLB package install (without Unity)

Download the W01/H01 pilot ZIP from the ChatGPT attachment. Run from the
Unity project checkout, replacing the ZIP path as needed:

    python Tools/ModelIntegration/install_pilot_assets.py --archive C:\Downloads\RoboRoguelike_VisualPilot_W01_H01_Assets.zip --project .
    python Tools/ModelIntegration/install_pilot_assets.py --archive C:\Downloads\RoboRoguelike_VisualPilot_W01_H01_Assets.zip --project . --install
    python -m unittest discover -s Tools/ModelIntegration -p "test_*.py" -v

The installer only writes the three specific SHA-256-verified GLBs under
Assets/RRLImportedModels. It refuses to overwrite different models and does not
modify Unity .meta or package dependencies. glTFast editor import, generated
meta files, Unity compilation and PlayMode remain REQUIRED and NOT RUN here.
