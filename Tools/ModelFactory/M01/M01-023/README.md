# MODEL-01 / M01-023 — Wieżyczka o profilu klinowym

**Catalog:** `feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md`.
**Claim:** `Docs/ModelFactory/claims/M01-023.json`, CREATE commit `fdec5158f4fe80a4dbb3f69fe33e7b3fd87064a6` (2026-10-25 09:46:49 UTC).
**Branch:** `feature/rrl-model-m01-023-wedge-profile-turret`, based on `develop` `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.

## Visual model

Low-profile wedge turret with **two separate faceted armor shells**, ceramic inset plates, hollow forward barrel, copper cooling vents, titanium yaw bearing and cyan muzzle/optics. Seven materials. Only visual geometry, **no combat mechanics**. The mounting origin is `MOUNT_TOP_SOCKET` at `(0,0,0)`, **Y up / +Z forward / metres**. The rotating group pivots at `(0,0.44,0)` via `WEDGE_TURRET_YAW`, performing a full 360-degree yaw over frames 1–73 at 60 FPS.

The stationary base ends at Y=0.356 m; the moving assembly begins at Y=0.455 m. Sampled swept AABB tests of all moving–fixed pairs at 73 poses validate the designed separation, but **do not prove absence of self-intersections between moving components** or continuous collision-free motion.

## Reproduce

```bash
python3 Tools/ModelFactory/M01/M01-023/qa_m01_023.py
blender --background --python-exit-code 1 --python Tools/ModelFactory/M01/M01-023/generate_m01_023.py -- --repeat --out Tools/ModelFactory/M01/M01-023/generated
python3 Tools/ModelFactory/M01/M01-023/qa_m01_023.py --out Tools/ModelFactory/M01/M01-023/generated --require-blender
```

With ordinary Python, the generator creates only real **OBJ/MTL/QA JSON**. Blender is required for **BLEND, FBX, animated GLB**; CI validates file signatures and the glTF rotation channel. GitHub Actions publishes exports as an artifact and attempts to commit them to this exclusive branch.

## QA gates

- Closed, oriented, positive-volume triangle meshes; no degenerate triangles.
- Distinct materials with separate metallic, ceramic and emissive accents.
- Root mount and animation pivot defined explicitly.
- 73 sampled yaw positions; conservative moving–fixed AABB separation.
- Independent OBJ/MTL reparse, deterministic SHA-256 on repeated Python runs.
- Blender repeatability: build twice in one process without leaked objects/materials (CI only).
- GLB 2.0 animation channel, FBX and BLEND signatures (CI only).

**NOT RUN until verified:** Unity 6 import, EditMode, PlayMode, in-engine animation/material review, continuous triangle collision detection, gameplay integration. **Draft / NO MERGE** until manual visual inspection and Unity import. Do not modify WPN-001..006, shared scenes, prefabs, `main`, `develop` or ProjectSettings.