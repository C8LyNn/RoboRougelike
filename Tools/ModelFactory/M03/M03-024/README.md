# M03-024 — Moduł mostkujący przerwę

**Catalog:** M03 / item 024, branch `feature/rrl-model-factory-queue-v1`. **Scope:** standalone visual modular bridge only; no gameplay code or shared Unity scenes.

## Geometry and mounts

- Root pivot: **(0, 0, 0)** at near-side frame mounting plane. Blender/Unity scale: **1 unit = 1 metre**. X is the bridge direction, Z up.
- Two outer graphite rectangular **hollow** beams: X 0.07–1.20 m, Y ±0.32 m, centre Z 0.22 m. Outer 174 × 214 mm, wall 18 mm.
- Two inner titanium **hollow** beams: X 0.55–1.74 m in the local sliding carriage. Outer 124 × 160 mm, wall 16 mm. Minimum radial clearance from outer shell: **7 mm Y / 9 mm Z**.
- Fixed and sliding crossbars, separate ceramic deck segments, rubber anti-slip pads, copper mounting pads and amber markings. Intended overlaps occur only at structural welds, attachment pads and the telescopic insertion.
- Four SNAP empties with local **+Z** pointing outward along **±X**. Near: `(0, ±0.32, 0.22)` parented to ROOT. Far: `(1.79, ±0.32, 0.22)` parented to SLIDING_CARRIAGE.
- Six materials: graphite, titanium, copper, ceramic, rubber, amber. No new Unity systems.

## Motion

`M03-024_SLIDING_CARRIAGE` translates along **+X**, **0.00–0.42 m**, animated at frames 1–110 (hold, extend, hold, retract). At full extension, insertion overlap remains **230 mm** (1.20 − 0.55 − 0.42). All far-side SNAPs move with the carriage. This is a visual animation; load-bearing physics must be validated by the game team.

## Generate / validate

```bash
blender --background --python-exit-code 1 --python Tools/ModelFactory/M03/M03-024/generate_m03_024.py -- --output /tmp/M03-024
blender --background --python-exit-code 1 --python Tools/ModelFactory/M03/M03-024/qa_blender_m03_024.py -- --output /tmp/M03-024
```

The generator exports `M03-024.blend`, `M03-024.glb` and `M03-024.fbx`. Independent QA verifies manifold positive-volume geometry, materials, SNAP transforms, 110 sampled animation frames, telescoping clearance/overlap, repeatable generation, preserved unrelated objects and actual binary GLB content.

**Unity 6000.3.14f1:** NOT RUN until actual Unity import, URP materials, animation, socket compatibility and collision validation are performed. Blender/CI outcomes must not be represented as Unity PASS. No modifications to ProjectSettings, shared scenes or existing mechanics.
