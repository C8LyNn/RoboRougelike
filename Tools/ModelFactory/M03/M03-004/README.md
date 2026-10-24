# M03-004 — Wzmocniona rama X (reinforced X frame)

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`.  
**Claim:** `Docs/ModelFactory/claims/M03-004.json` — created 2026-10-24T13:44:35.329Z by MODEL-03, control-branch commit `e0be41c6296ffe608969fa27b1d86e4281cc62f8`, claim blob `61071bffc95324c32ce57927d23b0b699ae1cb84`.  
**Scope:** one new visual structural module; no gameplay code, scenes or ProjectSettings changes.

## Construction and visual language

One metre modular **1×1×1 m** space-frame with two mirrored side-panel X bracing systems. Each X is four individually machined titanium-coloured diagonal ribs captured by a central orange bolted hub; eight diagonal arms in total. Graphite longitudinal rails, two front/rear centre-load bridges and top/bottom central spines carry the six real ceramic SNAP contact plates. The model deliberately includes small welded/bolted overlap at load-bearing joints. It does not claim Boolean-unioned manifold topology across separate beams.

**Orientation:** Blender local X right, Y forward, Z up; one unit = one metre. Bounds: X [-0.5,+0.5], Y [0,1], Z [-0.5,+0.5]. **Pivot:** rear SNAP port (0,0,0), a real mounting contact face rather than an arbitrary origin. Additional FRONT, LEFT, RIGHT, TOP, BOTTOM ports are stored as child Empty nodes with outward normals in the Blender source. The module is rigid: **animation not required by catalog**.

**Materials (4):** graphite structural steel, titanium diagonal braces, ceramic SNAP plates, safety-orange central bolted hubs. Blender version uses PBR metallic/roughness values and 6 mm, 3-segment edge bevels plus weighted normals; static OBJ/MTL uses the same source geometry without bevel modifiers.

## Reproducible generation

```bash
python3 Tools/ModelFactory/M03/M03-004/generate_m03_004.py --self-test --output-dir /tmp/m03-004
# With Blender installed:
blender --background --factory-startup --python Tools/ModelFactory/M03/M03-004/generate_m03_004.py -- --output-dir /tmp/m03-004-blender --require-blender
```

Files:
- `generate_m03_004.py` — deterministic geometry, pure Python OBJ/MTL, Blender `bpy` source and 6 SNAP Empty ports; reruns remove prior M03-004 collection.
- `M03-004.obj` — actual 32-part static 3D model with 256 vertices, 192 quads (384 triangles).
- `M03-004.mtl` — 4 materials.
- `M03-004.static-qa.json` — geometry validation, bounds, outward-facing quad normals and six ports.
- `.github/workflows/ci-model-m03-004.yml` — independent deterministic source/asset QA, Blender export when available in GitHub Actions.

## QA / limitations

**Locally executed 2026-10-24:** stdlib Python generator `--self-test` PASS, static OBJ and MTL exported; trimesh per-object analysis: **32/32 closed watertight cuboids**, **32/32 consistent winding**, **32/32 positive volumes**; all 192 outward face normals checked by generator; six port positions and 1m envelope validated. A local matplotlib visual inspection was performed, but **Blender visual QA: NOT RUN**.

**Blender 5.0.1 executed — PASS:** [GitHub Actions #40099997626](https://github.com/C8LyNn/RoboRougelike/actions/runs/40099997626) installed Blender and numpy, ran the real `bpy` source, generated both editable `.blend` and binary `.glb`, verified the GLB header, **32 GLB mesh nodes, 4 materials, and isolated M03-004-only export**. It also ran `export_blender()` twice in the same Blender process and confirmed **38 owned objects / 32 owned mesh datablocks** without growth. Artifact `M03-004-Blender-GLB` (ID `12562517239`, approximately 148 kB compressed) is downloadable from the workflow run. Python static geometry and deterministic OBJ/MTL checks also PASS in this run.

**Unity 6:** EditMode and PlayMode GitHub Actions stopped at the *Require valid Unity activation credentials* step before starting Unity ([EditMode](https://github.com/C8LyNn/RoboRougelike/actions/runs/40099879848), [PlayMode](https://github.com/C8LyNn/RoboRougelike/actions/runs/40099879819)). State **BLOCKED_UNITY_ACTIVATION**; Unity import, vehicle attachment and runtime visual rendering are **NOT RUN**. Existing intentional welded frame overlaps are not equivalent to unintended animation clipping; no animation exists here. The static OBJ is not Boolean-unioned and its 32 components are individually closed; physics colliders should be generated separately, not inferred from this visual asset.

**Safety:** This Draft PR targets `develop`, does not merge automatically, and does not modify shared scenes, physics systems, `Assets` or `ProjectSettings`.
