# ENV03-001 — Segment niskiego muru areny (pilot)

**Status:** isolated experimental environment art asset, **not integrated** into RoboRougelike.
**Owner:** ENV03 (single pilot); no other ENV03 models authorized.
**Related governance:** [#287](https://github.com/C8LyNn/RoboRougelike/issues/287), [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).
**Art review:** PENDING. No Codex handoff, Unity import, or gameplay modification.

## Purpose and visual language

One low, static, modular perimeter wall for an obstacle course or arena.
Industrial sci-fi graybox/low-poly: broad concrete mass, continuous dark steel
coping, small steel tie plates and safety-ochre reflectors. Silhouette and contrast
remain readable at gameplay distance. No damage states, construction/destruction
logic, animation, rig, gameplay scripts, or procedural level generation.

## Geometry contract

- **Scale:** 1 Blender unit = 1 metre.
- **Axes:** +X runs along the wall; +Y points away from the arena; -Y is the
  arena-facing side; +Z points up.
- **Root pivot:** `ENV03-001_ROOT` at world origin (0,0,0), ground-centred.
- **Overall AABB:** X -1.00..+1.00 m, Y -0.24..+0.24 m, Z 0.00..0.88 m.
- **Modularity:** end faces at X=-1.00 and X=+1.00 have matching cross-sections
  for both continuous extrusions. Put next instance at X=+/-2.00 m with no
  intervening gap. All decorative details stay away from mating ends.
- **Target topology:** 18 separate closed mesh objects, 504 triangles total,
  three simple PBR materials. No UV texture maps or external assets.
- **Materials:** `ENV03_Concrete`, `ENV03_DarkSteel`, `ENV03_SafetyOchre`.
- **Contact design:** coping touches body at Z=0.78 m; plates/reflectors touch
  the flat Y=+/-0.20 m concrete face; bolt bases touch steel plate surfaces.
  These are deliberate surface contacts, not interpenetrating decorative meshes.
- **Suggested collision (not implemented):** one box collider centered at
  (0,0,0.44) m with size (2.00,0.48,0.88) m. Conservative AABB includes the
  sloped concrete shoulders and protruding details; optimize only after
  gameplay acceptance. No physics claims.

## Reproduce with Blender 4.x

```bash
blender -b --python Tools/EnvironmentFactory/ENV03/ENV03-001/generate_env03_001.py -- --output-dir /tmp/ENV03-001
blender -b --python Tools/EnvironmentFactory/ENV03/ENV03-001/qa_env03_001.py -- --output-dir /tmp/ENV03-001
```

The generator writes `ENV03-001_ArenaWall.blend`,
`ENV03-001_ArenaWall.glb`, `export_manifest.json` and, if headless CPU
rendering succeeds, two PNG previews (`arena_front`, `rear_end`).
The independent QA writes `qa_report.json` and checks watertight positive-volume
meshes, material budget, triangle budget, scale, root, end symmetry, source
geometry, GLB import and PNG signatures. The isolated GitHub Actions workflow
uploads real generated files; source files alone are **not** evidence of export.

## Art review checklist

Inspect the two previews and the actual .blend/.glb before approving:
whether the concrete reads as a wall (not a vehicle part), the top metal line
is continuous across instances, the reflector contrast is appropriate,
and the shape reads clearly at the intended camera distance. The flat
rectangular collision suggestion must not be mistaken for a tested collider.

## Provenance and license

Original geometry and Python construction authored specifically for this pilot
without third-party mesh files, scans, branded textures, or copied competitor
assets. **License: project-internal/proprietary, rights reserved by the
RoboRougelike repository owner (C8LyNn), pending their final art/license
approval.** This does not grant a public redistribution license.
The generator depends only on Blender's built-in `bpy`/`bmesh` libraries.

## QA truth table

- Blender geometry, GLB import, export files, and preview: **only PASS after
  actual successful workflow execution**; otherwise NOT_RUN/FAIL.
- Unity 6 EditMode / PlayMode: **NOT_RUN**.
- Windows build / gameplay / manual visual review: **NOT_RUN / PENDING**.
- Integration with existing scenes, prefabs, Codex source: **NOT_AUTHORIZED**.

No merge or asset import is permitted without HQ approval.
