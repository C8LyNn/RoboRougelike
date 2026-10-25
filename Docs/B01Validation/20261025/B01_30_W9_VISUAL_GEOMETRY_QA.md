# B01-30 / W9 — VISUAL & GEOMETRY QA (CAMERA / BUILD / RAC)

**Data:** 2026-10-25 | **Owner:** B01-30 — 3D Models & Visuals  
**HQ dispatch:** W9 / CAMERA-001, BUILD-001, RAC-001 — oddzielne poprawione rewizje.  
**Scope:** read-only source examination and narrowly scoped visual QA recipes. No modifications to the Codex game, scene 1, physics, AI, garage, economy, generator or producer branches.  
**Unity:** **NOT_RUN** (no accessible licensed Unity 6000.3.14f1 editor or active Game view in the audit environment). **SCREENSHOTS/VIDEO: NONE**, therefore **no in-Unity visual defect is certified**; the findings below are either **STATIC_CONFIRMED** (behavior visible unambiguously in reviewed code) or **UNVERIFIED_VISUAL_RISK**.  
**Native W9 history:** original sources produced genuine native compile failures in QA PR #212; the repaired versions below have **not** been shown to compile/run natively. Code fixes are statically present but may not be treated as native PASS.

## Exact immutable inputs

| Package / producer revision | Source pull request / reviewed exact HEAD | Specific W9 repair |
|---|---|---|
| CAMERA-001 R1.1 | [PR #214](https://github.com/C8LyNn/RoboRougelike/pull/214) / `30c0419cb9c103303a9b4e301a241a7a3684ebd9` | Removed direct TestRunner assembly references in both EditMode/PlayMode asmdefs, kept `optionalUnityReferences:["TestAssemblies"]`. |
| BUILD-001 R1.2 | [PR #215](https://github.com/C8LyNn/RoboRougelike/pull/215) / `0788d0a8f4a40535e0c84e9ea9d7f2a6b5e8b267` | Same test asmdef correction. |
| RAC-001 R3.1 | [PR #216](https://github.com/C8LyNn/RoboRougelike/pull/216) / `129e867f25805075c48f2be52ec1cc5336b4075c` | Restored terminal `#endif // UNITY_EDITOR` in `RAC001DemoSceneBuilder.cs`. |

**Source integrity context:** CAMERA and BUILD corrected PRs touch only test asmdefs, manifests and source-repair documentation, so their actual runtime/demos retain original producer geometry and framing. RAC correction is a preprocessor closure; no scene geometry revision is claimed. The SHA listed for a report is the **reviewed source SHA**, not a new runtime bug-fix SHA.

## CAMERA-001: framing, obstruction and reticle

**Read-only inspected:** `Assets/B01Packages/CAMERA001/Runtime/Camera001Framing.cs`, `Camera001Rig.cs`, `Demo/Camera001Demo.cs`, PlayMode source tests and corrected test asmdefs at exact W9 SHA.

- **CAM-VIS-01 / P1 / STATIC_CONFIRMED (demo coverage gap):** `Camera001Demo.OnGUI` prints framing statuses (`OWN HULL IN RETICLE`, `WORLD CLIPPED`, `PIVOT OVERLAP`) but neither that demo nor the camera rig renders a visible crosshair/reticle. `Camera001Framing.Solve` calculates a center ray; this is **not** a graphical reticle. **Cannot test reticle pixel contrast, overlap or legibility in 720p/1080p on this isolated scene as delivered.**
  - **Exact evidence:** SHA `30c0419c...`, `Demo/Camera001Demo.cs` / `OnGUI`, `Runtime/Camera001Rig.cs` / `StepCamera`.
  - **Small proposed fix (owner B01-20, demo ONLY):** opt-in center-screen reticle draw with thin/high-contrast shape and dark outline; always render in 1280x720 and 1920x1080; keep the visual as UI only, never cast a second authoritative gameplay ray. No modifications to host camera, weapon aiming or combat HUD. Test actual screenshot+center pixel geometry.
- **CAM-VIS-02 / P1 / UNVERIFIED_VISUAL_RISK:** tall/short framing may yield `FitSaturated`, off-screen vehicle or excessive background at pitch extremes. The solver uses a bounding hull and caps distance; its `FitSaturated` flag must be checked, not counted as PASS. Static demo has Light width=2.1m, length=3.2m and weapon vertical extent reaching ≈4.8m; Heavy width=4.5m, length=5.8m and high element reaching ≈7.1m (local floor reference). The actual 720p/1080p framing has **not** been observed.
  - **Small proposed fix IF reproduced:** restrict or display `FitSaturated` warning in standalone demo; adjust camera fit clamps/headroom based on a measured failed case, not intuition.
- **CAM-VIS-03 / P1 / UNVERIFIED_VISUAL_RISK:** camera spherecast from aim to desired/smoothed pose intentionally ignores self-colliders and checks world geometry. There are three demo walls at X=4/8/12m, width .7m, height4m. Actual wall penetration, obstruction of subject, smoothing pop, and clipping at 720p/1080p remain **NOT_RUN**.
  - **Small proposed fix IF reproduced:** limit only the offending smoothing segment or add an explicit safe near-clip distance; do not deactivate wall colliders or rewrite the player camera.

**CAM visual test scenario (B01-60):** Open `CAMERA001_Demo.unity` at W9 SHA, set Game view 1280x720 and 1920x1080, swap Light/Heavy via TAB. For each, orbit yaw 0/90/180/270°, test pitch -8/18/70°, zoom 3/8/18m; include near walls X=4/8/12 and record centered reticle, full hull visibility, weapon visibility, camera-wall intersections and diagnostic flags. Capture named PNG before/after each failing setup and a short video of continuous yaw/zoom transitions; report exact Unity version, test SHA, frame dimensions, parameter values and **OBSERVED** results. The demo's own HUD is not proof of visible reticle quality.

## BUILD-001: socket patches, axle orientation and valid/invalid preview

**Read-only inspected:** `Assets/B01Construction/BUILD001/Runtime/BuildSocketGeometry.cs`, `BuildPlacementRules.cs`, `BuildSocketRaycast.cs`, `Demo/Build001Demo.cs`, physical-hit test source and corrected test asmdefs at exact W9 SHA.

- **BLD-VIS-01 / P1 / STATIC_CONFIRMED (demo coverage gap):** socket faces/half-size/typed kind exist only as `BuildSocket` data. `Build001Demo` instantiates `LogicalCore_OneOwner` and `BUILD001_GHOST_PREVIEW` as featureless cubes; no socket face outline, patch plane, hub/axle directional guide or wheel model is drawn. It is thus impossible to **visually judge axle alignment and wheel mounting** from the demo alone; boolean rules/physical ray tests do not substitute.
  - **Exact evidence:** SHA `0788d0a8...`, `Build001Demo.Start/Cycle`; `BuildSocketGeometry.BuildSocket` and `BuildFixtureCatalog.Wheel`.
  - **Small proposed fix (B01-20, demo ONLY):** add `Gizmos` and/or optional non-colliding line/axis indicators for each logical socket, with distinct tags for `Structural/Axle/WeaponBase/SuspensionBase`; axle +/- axis arrow derived directly from transformed `BuildSocket.Face` and placement pose. No physics/hitbox replacement or automatic sockets from art.
- **BLD-VIS-02 / P2 / STATIC_CONFIRMED (graybox correspondence gap):** the central core cube and ghost cubes are `.52m` wide, while the mounting grid center separation is `.55m`. For an adjacent valid attachment, visible unrotated cubes have a **0.03m (3cm)** face-to-face gap even though logical socket planes meet at the grid boundary. This is not by itself a collision defect, but a visual demonstration of a supposedly touching assembly can be confusing.
  - **Exact evidence:** `Build001Demo.cs` `localScale=Vector3.one*.52f`; `BuildSocketGeometry.GridMetres=.55f`.
  - **Small proposed fix (demo ONLY):** leave physics and logical 0.55m grid unchanged; show a narrow, non-colliding mounting bridge/patch at the real socket plane or place transparent socket-face quads matching the calculated patch (not an authored extra hitbox). Validate no new collider and no fake geometric attachment.
- **BLD-VIS-03 / P1 / UNVERIFIED_VISUAL_RISK:** preview material is green for `Valid`, red otherwise via `renderer.material.color` but the demo does not provide an explicit visual axis or typed socket reason at the connection point. Color visibility and shader compatibility in URP 720p/1080p are **NOT_RUN**; the textual `verdict` is useful but does not reveal *which* face rejected the placement.
  - **Small proposed fix IF needed:** use an outline/shape indicator and localized label tied to the same verdict (not color alone); confirm shader/material before adding additional assets. Keep rejection state authoritative in `BuildPlacementRules`.
- **BLD-VIS-04 / P2 / STATIC_CONFIRMED (demo ordering):** `Cycle()` increments `choice` before the first preview (`choice` defaults to zero), so the initial candidate uses `case 1` (wheel on top) rather than the intended `case 0` hub candidate. This makes opening the scene begin on an invalid demonstration. **This is an ordering observation, not a measured gameplay failure.**
  - **Small proposed fix:** initialize `choice=-1` (or advance only on button press); UI initial state should say which case is active.

**BUILD visual test scenario (B01-60):** Run `BUILD001_Demo.unity` at both 1280x720 and 1920x1080; cycle all five candidate states, log `definition.Id`, `pose.Anchor`, `verdict`, face normal, patch center and world grid. Review valid hub, invalid tire top, gun base/barrel, armor face; check that **rendered wheel axle** and **logical axle** point in the same direction, red/green remain distinguishable in color-vision-deficiency simulation if available, visual/physics geometry remains separate. Save screenshots at each case and an optional side view close-up, not a mere source-unit-test log.

## RAC-001: checkpoint height, path readability and visual/physical separation

**Read-only inspected:** `Assets/RoboRougelikeMechanics/RAC001CheckpointRace/Editor/RAC001DemoSceneBuilder.cs`, `CheckpointRaceDemoController.cs`, `CheckpointRaceChallenge.cs` at exact W9 SHA.

- **RAC-VIS-01 / P1 / STATIC_CONFIRMED (height):** the floor top is **Y=0m** (`Cube` center -0.5m, height1m). Checkpoint Cylinders are placed at Y=0 with Y-scale .05. Default Cylinder mesh is 2 units tall; markers have height **0.10m**, spanning Y≈-0.05..+0.05m: **50% embedded below the floor**, leaving a 5cm-high top surface. In Unity lighting this is likely to look like a shallow disk and may be hard to see from the fixed distant camera, but *visibility remains NOT_RUN*. Positions: CP1 (8,0,0), CP2 (8,0,8), CP3 (0,0,8).
  - **Small proposed fix (B01-20, scene builder ONLY):** position marker center at Y=.05m (bottom flush with floor, top=.10m), or use a flat noncolliding projected ring above floor, while keeping checkpoint **logical Transform center** at originally intended gameplay Y=0 via a parent data Transform + a separate visual child. Do not move the authoritative race target without revalidating the scoring radius.
- **RAC-VIS-02 / P1 / STATIC_CONFIRMED (route cues):** all three cylinders are named `Checkpoint 1/2/3` in hierarchy but have no visible in-game index, direction arrows, ordered path stripe, or distinct active-next color. `CheckpointRaceDemoController.OnGUI` shows current index/time but does not identify the next physical ring on screen.
  - **Small proposed fix (demo ONLY):** optional non-colliding numbered/ordered labels and subtle route arrows generated from the checkpoint array. Do not add scoring triggers or authored physical obstacles.
- **RAC-VIS-03 / STATIC_CONFIRMED (separation is present):** `marker.GetComponent<Collider>().enabled=false` immediately disables each Cylinder collider. The standalone floor and Rigidbody sample retain their physical colliders. This is the correct direction: *logical race scoring uses checkpoint Transform positions/sphere crossing rather than the decorative marker collider*. However no physical PlayMode regression was executed, so **real-world no-obstruction = NOT_RUN**.
  - **Regression risk:** a later visual prefab could inadvertently enable a Collider or move the target Transform with cosmetic child offsets. Count enabled colliders on marker descendants and assert no Rigidbody impulse due to markers.
- **RAC-VIS-04 / P2 / UNVERIFIED_VISUAL_RISK:** fixed camera at (4,17,-14) looks at (4,0,4); no line/trail of the rectangular CP route; no light is explicitly instantiated by the empty additive scene builder. Actual framing, material brightness, and scene-camera conflicts in a project with other loaded cameras require Game view proof. Do not label this a visual FAIL from code alone.
  - **Small proposed fix IF observed:** one scene-local directional light and temporary line/path cue, with no collider and no game-scene edits; or separate test of standalone vs additive multi-camera visibility.

**RAC visual test scenario (B01-60):** In an isolated Unity editor, use `Tools/RoboRougelike/RAC-001/Open Unsaved Demo (Additive)`; verify scene unsaved and physical floor, CP positions Y=0 and visible marker bottom/top relative to floor. At 1280x720 and 1920x1080, record before Start, midway CP1/CP2/CP3, after Reset. Check route ordering, marker visibility, no collider contact, Rigidbody vertical position and physics motion, physical floor collider and decorative marker collider enabled=false. Capture one hierarchy/Inspector screenshot showing collider state plus Game view shots. Review two cameras if host scene remains open. No Codex host worktree edits.

## Required immutable visual evidence per confirmed Unity defect

For each **actual observed Unity visual failure**, B01-60 or operator must attach:
1. **Source exact HEAD SHA** (from table above) + Unity 6000.3.14f1 and source checkout identity; no reuse of previous SHA screenshots.
2. **PNG at 1280×720 and 1920×1080** or a clip with frame size evident; shot must contain the defect, not only labels/status text.
3. **Minimal scenario:** scene, step sequence, vehicle type, angles/settings or checkpoint/socket ID and world position, observed vs expected, frequency.
4. **Small proposed fix** confined to isolated package visual/demo layer, and **new patch SHA** only once a real correction is committed; screenshots before/after and independent retest must point at their respective source SHAs.

**At report creation:** screenshot/recording files **NOT_AVAILABLE**; no new corrected code SHA because **no visual-runtime defect was confirmed in Unity and no gameplay or demo correction was committed**. This documentation-only revision's commit SHA is *not* a substitute for a corrected package SHA.

## Requested follow-up and gate

- **B01-20:** prioritize optional demo-only reticle, socket/axle overlay, checkpoint labels/marker-offset separation. Each edit requires a small isolated source revision SHA and preserved authority: camera collision, socket validation, checkpoint scoring unchanged.
- **B01-60:** run licensed Unity corrected sources, compile, EditMode/PlayMode, then 720p/1080p visual test matrices; attach actual log/XML/screenshots. On any FAIL, do not claim PASS until new patch HEAD independently retested.
- **B01-40:** independently check the corrected package manifests/meta and future revision dependencies. Native compile of original W9 FAIL sources is **not** proof of the corrected branches.
- **B01-00:** authorize gate. NO MERGE, no Codex-worktree changes, no GPT OSS/Codex integration inferred.

**Gate:** `B01_30_W9_STATIC_REVIEW_DONE / CAMERA_VISUAL_NOT_RUN / BUILD_VISUAL_NOT_RUN / RAC_VISUAL_NOT_RUN / NATIVE_REPAIRS_NOT_RUN / G2_HOLD / NO_MERGE`.
