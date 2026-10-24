# M03-006 — Uchwyt boczny L

**Model Factory M03 / 24.10.2026.** Side-mounted structural L bracket, visually distinct chamfered corners. Asset only; no gameplay, scene, ProjectSettings or existing integration edits.

## Geometry

- Unity scale **1 unit = 1 metre**; mounting plane X=0, local pivot **(0,0,0)**; side mounting normal **+X**, upright +Z.
- Main monolithic L section with bevelled outer corner and reinforced inner bend; **two 4-hole perforated mounting flanges** with actual open bores (not fake textures). Side flange X=-0.04..0.10 m, top flange Z=0.52..0.64 m.
- Second mounting pivot **(0.64,0,0.58)**, normal +Z; placement is intentionally not a full 1×1×1 frame, but an adapter between side and top module sockets.
- **27 watertight components / 1764 vertices / 3452 triangles / 5 PBR materials.** Graphite load frame, titanium flanges/gussets, ceramic guards, copper fasteners, amber hazard strips.
- Eight real 0.023 m-radius bolt shafts in 0.031 m-radius mounting bores, radial clearance **8 mm**. Bolts, structural reinforcements and guard overlays intentionally contact the structure; no other intersections are intended.
- Fixed bracket: **no animation required**. No physics collider authored; integration owner should create a simplified collision shape and configure SNAP points.

## Files and usage

`geometry.json` is the complete deterministic geometry source (mesh vertices, outward-facing faces, material IDs, two mounting pivots). `generate_m03_006.py` reads it, validates manifold topology and exports OBJ/MTL without dependencies. When executed under Blender 4.2+ with `bpy`, it additionally reconstructs every mesh, creates root and top socket empties, exports **model-only GLB**, and saves an appendable `.blend` collection library. It removes only its own `MODEL_M03_006` collection and restores prior scene selection/frame. Blender exporter is not required to regenerate the checked-in static asset.

```sh
python Tools/ModelFactory/M03/M03-006/generate_m03_006.py --self-test
python Tools/ModelFactory/M03/M03-006/generate_m03_006.py --out build/M03-006
# Optional:
blender --background --python Tools/ModelFactory/M03/M03-006/generate_m03_006.py -- --out build/M03-006
```

`generated/M03-006_side_L_bracket.obj` and `.mtl` are real generated static 3D files. The CI workflow regenerates them byte-for-byte, checks the 27 closed components and verifies the mount data. A GLB is only claimed if Blender actually runs.

## QA and integration gate

**Static geometry generation and mesh topology: PASS** (27 closed components, positive volume, 8 drilled holes, 8 fasteners). **Blender bpy export: NOT RUN locally. Unity 6 import/PlayMode: NOT RUN.** GitHub Actions will independently verify deterministic output. Actual visual inspection and runtime Unity test remain integration gates; keep PR DRAFT until these are done.

**Claim:** `Docs/ModelFactory/claims/M03-006.json` on `feature/rrl-model-factory-queue-v1`, CREATE commit `9a3dc1c09f8e8976c1d6cc15d71414c72dee7a94`, blob `d2f5ec341a27b51dfd8686991ef74822a1551b47`. Original claim timestamp 2026-10-24T15:50:40.927Z. No other M03 IDs are modified.
