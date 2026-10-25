# MODEL-01 / M01-019 — Wyrzutnia dymna sześciotubowa

Source of truth: `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. Claim: `Docs/ModelFactory/claims/M01-019.json`, CREATE commit `204db2e2298849faffa55336bd3630299699720f` at 2026-10-25T05:47:15.186Z.

## Visual design

Six asymmetrically sized **genuinely hollow** smoke tubes in an indexed cassette. Distinct muzzle lengths and diameters, two support rings, graphite chassis, ivory ceramic armor, copper trim and radial spokes, orange safety markers and cyan status accents. Seven PBR materials. Visual-only; no WPN mechanics.

## Coordinate and animation contract

- 1 Blender unit = 1 metre. Blender native Z-up, tubes point -Y. FBX/GLB configured for Unity Y-up / +Z-forward.
- `MOUNT_SIDE_SOCKET` is at (0,0,0), X=0 side mounting plane. Structural arm extends along +X.
- `CASSETTE_INDEX_Y` pivot at (0.66,-0.13,0.48), local Y rotation axis. Six 60° index steps, 73 frames, **60 FPS** (Blender scene setting), with dwell phases. End angle 360°.
- Animated cassette is separated from the stationary rear bearing. QA samples 73 frames and checks every moving/static AABB pair; this is not continuous triangle-triangle collision detection.
- Static OBJ/MTL do not carry animation; GLB/FBX do.

## Build and validate

```sh
blender --background --python-exit-code 1 --python Tools/ModelFactory/M01/M01-019/generate_m01_019.py -- --out Tools/ModelFactory/M01/M01-019/generated --repeat
python3 Tools/ModelFactory/M01/M01-019/qa_m01_019.py --out Tools/ModelFactory/M01/M01-019/generated
```

The Blender exporter produces `.blend`, animated `.glb` and `.fbx`, static `.obj`/`.mtl`, and `_qa.json`. These must only be called generated after an actual successful Blender run. Unity import, EditMode/PlayMode, integration and manual visual review are NOT RUN unless separately evidenced.

## Isolation

Dedicated new branch from `develop`, own model directory and own workflow only. Draft PR to `develop`, no merge. Do not modify existing WPN-001..006, other models, shared scenes, prefabs, or ProjectSettings.
