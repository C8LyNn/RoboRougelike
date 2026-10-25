# M03-023 — Blok montażowy 2×2

**Agent:** MODEL-03. **Catalog:** `feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md`. This is an isolated modular **visual asset**, not a gameplay change.

## Dimensions and assembly

- 2 × 2 × 2 metres; Unity convention 1 unit = 1 metre. X/Y [-1,1], Z [0,2].
- ROOT pivot (0,0,0) is on the physical bottom mounting plane, at the bottom socket centre. Six SNAP empties at centres of faces PX, NX, PY, NY, PZ, NZ; local +Z of each empty is rotated to the outward face normal.
- 12 hollow graphite edge rails, 8 titanium corner hubs, 24 hollow diagonal face braces, 6 open octagonal copper socket collars, 6 ceramic liners, 6 rubber seals and 24 amber direction markers. **86 independently closed mesh components and 6 PBR materials.**
- Real central annular bores, not texture-based holes. Cross-braces meet corner hubs. Intentional structural contacts at weld joints; a per-mesh manifold test does not prove the entire assembled model is collision-free.
- Static model; no animation required by catalog. No changes to gameplay, shared Unity scenes, prefabs or ProjectSettings.

## Blender build and QA

```bash
python3 -m py_compile Tools/ModelFactory/M03/M03-023/*.py
blender --background --python-exit-code 1 --python Tools/ModelFactory/M03/M03-023/generate_m03_023.py -- --output /tmp/M03-023
blender --background --python-exit-code 1 --python Tools/ModelFactory/M03/M03-023/qa_blender_m03_023.py -- --output /tmp/M03-023
```

Generator outputs `M03-023.blend`, `M03-023.glb`, `M03-023.fbx`. Independent QA validates closed manifold geometry, positive signed volume, six correctly oriented face sockets, six materials, deterministic rebuilds with a foreign sentinel preserved, and the actual GLB header/JSON. Dedicated CI uploads files and SHA256SUMS only after successful checks.

## Unity handoff and limitations

Unity import, URP materials, visual silhouette, socket-to-prefab compatibility, rigidbody/collider fit and full triangle-level interpenetration checks must be verified separately. If Unity or Blender is unavailable, report NOT RUN; do not infer PASS from source inspection. Keep the PR DRAFT pending independent review.
