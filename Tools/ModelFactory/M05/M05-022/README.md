# M05-022 — Moduł odzysku energii kinetycznej

**Visual-only Blender 3D model**, not a gameplay energy-recovery implementation. Queue: M05. Claimed on `feature/rrl-model-factory-queue-v1` via CREATE, commit `7df14c478c2f81822f2ac2fb817b9643ffcfe3ac`, at 2026-10-25T01:42:31.895Z.

## Design / geometry
- Internal frame mount, **1 unit = 1 m**, Blender Z-up. Root `MOUNT_ROOT` at **(0,0,0)**. Shaft runs on **local +Y**.
- Visible weighted flywheel with open eight-spoke structure, ceramic/graphite rim weights, copper concentric traces, central shaft, bearing shells and two cage collars.
- 9 materials: graphite, titanium, steel, copper, ceramic, rubber, smoked glass, cyan and amber emissive LEDs.
- Rotor pivot `PIVOT_FLYWHEEL_Y` at **(0,0,0.62)**, keyed **0° → 180° → 360°** on frames 1/31/61, 30 fps interpretation left to Unity. Root and shaft sockets are explicit empties.
- Guard's inner radius is **0.405 m**; rotor is designed within **0.365 m**. QA measures world-space transformed mesh vertices across 61 frames; this verifies rotor/guard radial separation, not every triangle-pair collision.
- Intended internal-frame module. No energy economy, wheel braking, physical flywheel torque, VFX system or other mechanics added.

## Generate
```sh
blender -b -t 2 --python Tools/ModelFactory/M05/M05-022/generate_m05_022.py -- --out /tmp/m05-022
blender -b -t 2 --python Tools/ModelFactory/M05/M05-022/test_repeatability_m05_022.py
python3 Tools/ModelFactory/M05/M05-022/validate_m05_022.py --out /tmp/m05-022 --require-export
```

Expected outputs: `M05-022.blend`, `M05-022.glb`, optional `M05-022.fbx`, `M05-022_qa.json`. Generated binaries are CI artifacts, not committed into Unity scenes. GLB and BLEND are mandatory in CI. Blender and Unity are **NOT RUN until workflow results confirm otherwise**.

## QA and isolation
Blender QA: manifold mesh topology, 61-frame swept rotor/guard radial separation and floor clearance, repeated generation with stable names/materials, preservation of foreign objects. Independent stdlib validator checks binary GLB animation/materials/mount markers. Full pairwise mesh collision QA, Unity 6 import, EditMode, PlayMode and URP visual inspection: **NOT RUN** unless subsequently executed. Branch isolated from `develop`; Draft PR only; do not merge without integration review.
