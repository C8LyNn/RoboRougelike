# M05-021 — Sygnałowy maszt składany (visual only)

**Catalog:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`.  
**Claim:** `Docs/ModelFactory/claims/M05-021.json`.  
**Agent:** MODEL-05 / M05. **Target:** Unity 6 URP, visual prefab only.

## Design
Industrial telescopic signal mast, top-socket mounting. Graphite housing and rubber seals, titanium/steel telescoping tubes, copper cable details, ceramic service plate, three illuminated signal rings and amber beacon in a four-rod guard. Eight Blender materials including emissive cyan and amber. No radar, scanning, signaling gameplay, or new systems are implemented.

## Units, hierarchy and animation
1 Blender unit = 1 metre. `M05-021_MOUNT_ROOT` is the world origin (0,0,0) and actual mount point. Local +Z is extension/up; local -Y is forward. `PIVOT_MIDDLE_STAGE` and `PIVOT_INNER_STAGE` are nested empties with local (0,0,0), each animating along its parent's local Z. The top beacon and three LED rings follow the inner stage.

The middle tube extends 0.50 m between frames 20–55, then the inner tube extends 0.50 m between frames 55–95. Each retains at least 0.24 m axial engagement in the previous tube. Radial gaps are 10 mm and 8 mm. At frame 95 the beacon cap top is approximately 3.14 m above the mounting origin. Markers: `MOUNT_CABLE_IN`, `MOUNT_SERVICE_LEFT`, `MOUNT_SERVICE_RIGHT`, `MOUNT_BEACON_TOP`.

Tube walls are modeled as closed annular shells, rather than overlapping solid cylinders. Decorative fasteners, collars and supports may intentionally meet at attachment interfaces; these are not validated as a complete triangle-level collision sweep.

## Build
```bash
blender -b --python Tools/ModelFactory/M05/M05-021/generate_m05_021.py -- --out /tmp/M05-021
python3 Tools/ModelFactory/M05/M05-021/validate_m05_021.py --out /tmp/M05-021 --require-export
```
Generator saves `M05-021.blend`, exports `M05-021.glb` (required), attempts `M05-021.fbx` (optional) and writes `M05-021_qa.json`. It deletes only its own tagged objects and unused prefixed materials on rerun, retaining unrelated scene objects. Blender 4.x/5.x is targeted; FBX exporter may be unavailable on some installations.

## QA scope
The dedicated `test_repeatability_m05_021.py` checks two builds in one Blender scene, stable object and material counts, and preservation of unrelated objects/materials. The generator checks every mesh for manifold edges using bmesh, evaluates all 95 animation frames and validates stage engagement and radial clearance. The independent standard-library validator checks GLB v2 structure, mesh/material/animation presence and pivots. It does **not** prove full surface-to-surface noninterpenetration, texture correctness, Unity import compatibility or URP visual fidelity. These require separate tests. Do not claim Blender, GLB or Unity PASS unless a real runner completes the corresponding test.

**Repository safety:** Only this M05-021 package and its dedicated workflow are in the model branch. No scene, ProjectSettings, existing mechanics or other model packages are modified.
