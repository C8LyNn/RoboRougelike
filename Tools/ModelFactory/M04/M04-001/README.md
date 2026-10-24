# M04-001 — Wirnik boczny w osłonie

**Agent:** MODEL-04 / mobility special  
**Catalog ID:** M04-001  
**Catalog:** [CATALOG_V1.md](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md)  
**Reservation:** [M04-001.json](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/claims/M04-001.json)  
**Reservation commit:** `13dd5065c89ab3282e0bf6e6cb4261d3d21e2297`  
**Purpose:** visual-only modular side ducted rotor; no gameplay, prefabs, shared scenes or ProjectSettings changed.

## Design

- Mount plane at Blender X=0, rotor faces **+X (side mount, not top mount)**.
- One ducted five-blade propeller, radial duct inner radius **0.345 m**, theoretical blade tip envelope **0.307 m**, radial clearance **0.038 m**.
- Five PBR materials: graphite, ceramic, copper, rubber, teal emission. The casing, lips, mounting plate, bolts, bearing, struts and hub are separate mesh objects.
- Root object: `M04_001_ROOT_SIDE_MOUNT`; attachment marker: `SOCKET_SIDE_X_POS`; rotating pivot: `ROTOR_PIVOT_X`.
- 48 frames / 2 revolutions at 24 fps; rotor keyed every three frames (45 degrees) to preserve turns when exporting quaternion animation. For continuous playback, enable loop in Unity.
- Blender coordinate system Z-up, Unity/glTF Y-up. X mount normal stays +X.
- Approximate size including guard lights: **0.46 m deep × 0.88 m × 0.88 m**; origin at mount plane.

## Generate

Requires **Blender 4.x** with its bundled glTF exporter. No external Python packages.

```bash
blender --background --python Tools/ModelFactory/M04/M04-001/generate_m04_001.py -- --out ./build/M04-001
```

Expected output:
- `build/M04-001/M04-001_side_ducted_rotor.glb`
- `build/M04-001/M04-001_manifest.json`

For visual inspection without exporting, add `--no-export` and run in Blender's Python environment. The generator replaces only its own `RRL_M04_001_SideDuctedRotor` collection, not other scene contents. On repeated executions, it reuses named materials and replaces its own objects.

## Unity 6 import

1. Import the generated `.glb` using a compatible glTF importer (or convert to FBX with Blender if your project does not support direct GLB import). **No importer was added to this PR.**
2. Use `SOCKET_SIDE_X_POS` as the vehicle-side attachment reference; align its +X normal outward.
3. Set the animation clip for `ROTOR_PIVOT_X` to loop. Only the rotor shaft/hub/blades are animated; the guard and mount stay static.
4. Configure gameplay thrust, rigidbody and collision separately; this is visual geometry only.
5. Confirm normal orientation, emission under URP, loop continuity, collision envelope and material slots in the actual Unity project.

## QA evidence (this PR)

| Check | Status | Evidence / limit |
|---|---|---|
| Queue claim uniqueness | **PASS** | New GitHub CREATE succeeded; claim SHA above |
| Generator file committed | **PASS** | Confirmed commit on dedicated branch |
| 5 material definitions | **PASS (static)** | Named PBR materials in script |
| Side mount axis | **PASS (static)** | Root and socket at X=0; rotor rotates about X |
| Radial duct/blade envelope | **PASS (analytic)** | 0.345 - 0.307 = 0.038 m clearance |
| Animation authoring | **PASS (static)** | X-axis keys 1..49, step 3; runtime export unverified |
| Blender generator execution | **NOT RUN** | Blender / bpy unavailable in current runner |
| GLB export and geometry validation | **NOT RUN** | Requires Blender 4.x |
| Unity 6 import, animation and collisions | **NOT RUN** | Requires Unity project/editor |
| Visual appearance and mesh inspection | **NOT RUN** | Requires Blender render/Unity |

**Important:** The PR contains a reproducible source generator, **not a prebuilt GLB**. No claim of a completed binary asset, Blender PASS or Unity PASS is made.

## Follow-up validation gate

Before merge, run the command above twice in Blender, compare generated object counts, inspect the rotor at frames 1, 7, 13, 25 and 49, check the minimum blade/duct clearance, validate the exported GLB in an independent viewer and import into Unity 6 URP. Keep the PR in draft until those checks pass.
