# M04-014 — Napęd ślimakowy do błota

Isolated **visual-only** Unity 6 asset for the MODEL-04 queue. No vehicle controller, physics, scene, or ProjectSettings changes. Source of truth: Docs/ModelFactory/CATALOG_V1.md on feature/rrl-model-factory-queue-v1.

## Silhouette / mounting

A single three-turn, thick-walled helical mud screw (auger), cylindrical graphite drive core, brushed steel flight, exposed copper bearing races, front/rear open guard rings, longitudinal titanium cage rails, ceramic wear lips, two status lights, and a wide underside saddle. **Eight separate PBR materials**; emissive cyan/amber only as accents. The spiral is genuine watertight 3D mesh, not a texture or flat ribbon.

Coordinate system: **1 unit = 1 m; +Y up, +Z vehicle forward, +X right**. MOUNT_UNDERSIDE_Y0 is the mounting pivot at (0,0,0) on the underside of the frame. All geometry lies at Y <= 0. The ROTOR_Z_AXIS pivot is (0,-0.36,0.52); five moving meshes are parented to it. A complete turn is keyed over frames 1–121 at 30 FPS, with intermediate 90° keys. The motor can be mirrored or duplicated by the consuming vehicle assembly, but this PR adds only one screw model.

**Clearance design:** 263 mm max auger-flight radius vs. 296 mm minimum guard inner radius (33 mm radial clearance); 32.5 mm minimum axial clearance to guard rings; bearings have 55 mm inner radius around a 48 mm shaft (7 mm clearance). Longitudinal rails lie outside the flight envelope. The rotor core and flight intentionally meet as an assembly; the QA envelope concerns rotor vs. stationary guards. No actual Unity physics collision validation is claimed.

## Reproducible generation

Static Python (no Blender required):

```bash
python3 Tools/ModelFactory/M04/M04-014/generate_m04_014.py --self-test
python3 Tools/ModelFactory/M04/M04-014/qa_m04_014.py
python3 Tools/ModelFactory/M04/M04-014/generate_m04_014.py --out build/M04-014
```

Actual Blender generation, animated GLB and FBX exports:

```bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-014/generate_m04_014.py -- --out build/M04-014
python3 Tools/ModelFactory/M04/M04-014/qa_m04_014.py --glb build/M04-014/M04-014_mud_screw_drive.glb
```

Expected outputs: .obj, .mtl, .blend, .glb, .fbx, and M04-014_manifest.json under build/M04-014/ (the .blend/.glb/.fbx files are generated only by Blender). Re-running the script with Blender resets the scene before generation. Static QA checks actual OBJ contents, deterministic byte-for-byte outputs, watertight triangle manifold, consistent face winding, positive signed volume, motion envelopes and guard clearances.

## QA truthfulness

- **PASS (local Python):** geometry self-test, 36 meshes, 2788 vertices, 5472 triangles, 8 materials, 70,180 sampled spiral-vertex animation checks; independent OBJ/MTL determinism and coordinate verification.
- **NOT RUN at source creation:** actual Blender .blend/GLB/FBX, Unity 6 EditMode/PlayMode, Unity import, visual inspection, collision physics. CI will attempt Blender; only a completed successful workflow can upgrade that status.
- **Limitations:** Envelope checks are conservative analytical/static checks, not an exhaustive triangle-triangle collision solver. Confirm animation, axis conventions, shading, and appearance in Unity before acceptance.
