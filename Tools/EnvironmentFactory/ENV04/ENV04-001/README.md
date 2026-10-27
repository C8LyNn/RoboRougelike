# ENV04-001 — Kratka wentylacyjna / industrial ventilation grate

**Status:** `ART_REVIEW_PENDING` after verified Blender export, otherwise `SOURCE_ONLY`. One original experimental ENV pilot, independent of the exhausted M01–M05 model queues and the two active gameplay implementation packages. **Not integrated into Unity.**

## Design
A compact freestanding industrial sci-fi ventilation crate for obstacle-course or arena dressing: low rectangular dark steel base, four structural posts, raised silver rim and six large stationary canted louvres over a recessed dark well. Four small amber safety markings aid readability from the gameplay camera. The visible slits are decorative only; **no air physics, fan, damage, hazard, rig or animation**.

## Units, axes, pivot and collider proposal
- **Blender:** 1 unit = 1 metre; +Z up, +Y front, +X width.
- **GLB:** Blender `export_yup=True` converts +Z up to glTF +Y up; Blender +Y front maps to glTF -Z front. Unity import axis conversion **NOT_RUN**.
- **Pivot:** empty `ENV04_001_ROOT` at world (0,0,0), projected centre of the crate at ground level. Meshes are children of this empty.
- **Envelope:** approximately 1.56 m wide × 1.02 m deep × 0.53 m high (precise measured AABB in Blender QA).
- **Proposed collision:** one axis-aligned `BoxCollider` in Blender frame, centre (0,0,0.27) m, size (1.65,1.08,0.58) m. This is a **DESIGN_PROPOSAL**, not a PhysX test. If ever imported, convert coordinates to Unity correctly and verify.
- **Geometry budget:** 200–1500 triangles; at most 3 basic materials (graphite powdercoat, brushed steel, safety amber). No external textures.
- **Animation:** `ANIMATION_NOT_APPLICABLE`.

## Reproduce in Blender
```bash
blender --background --python-exit-code 1 --python Tools/EnvironmentFactory/ENV04/ENV04-001/generate_env04_001.py -- --out Tools/EnvironmentFactory/ENV04/ENV04-001/generated
blender --background --python-exit-code 1 --python Tools/EnvironmentFactory/ENV04/ENV04-001/qa_env04_001.py -- --out Tools/EnvironmentFactory/ENV04/ENV04-001/generated
```

The generator writes an actual native `.blend`, static `.glb`, two 960×640 Workbench PNG views and `build_report.json`. Independent QA reopens the BLEND, validates manifold and positive-volume geometry, materials, triangle count, pivot, GLB header/JSON, previews and file hashes, then writes `qa_report.json`. The isolated CI workflow writes `SHA256SUMS`, uploads artifacts and attempts to commit the actual exports **only to this branch**. Until CI has run successfully, do not claim that Blender files exist.

## Origin, rights and scope
Original geometry and scripts authored for RoboRougelike. No competitor mesh, model, texture, branding or reference asset was copied. Released under **CC0-1.0** (see `LICENSE.txt`). This experimental static concept is outside the two implementation streams only while it remains independent of gameplay and Unity. No edits to scenes, prefabs, ProjectSettings, gameplay, Codex packages or other model directories.

## Acceptance and limitations
Required: physical Blender exports, QA PASS, actual preview images and independent manual visual review by HQ. **Unity EditMode / PlayMode / URP / Windows / PhysX / player acceptance: NOT_RUN.** Draft PR only, no merge. No additional ENV04 model after this pilot; await manual art review.
