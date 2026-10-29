# ENV02-001 — terrain ramp, v002 source revision (concept only)

**Ownership:** ENV02 producer, original Draft PR #290, existing claim ENV02-001.
**Status:** REFINEMENT_V002_SOURCE / INDEPENDENT_ART_QA_PENDING. No Unity integration, Codex ACK or merge.
**History:** v001 source commit `daedcd618d3426ceffea0d1d093fdc3159844c90` remains immutable and downloadable through the [original Blender Actions evidence](https://github.com/C8LyNn/RoboRougelike/actions/runs/40484667955/artifacts/12723734098).
This is an **in-place source revision on the same owned branch**, not a new asset, claim or PR. All generated outputs use `_v002` filenames; no v001 receipt is overwritten.

## Geometry: actual changes from v001

| Parameter | v001 verified physical geometry | v002 source design (verify in new Blender artifact) |
|---|---:|---:|
| Length / drive width | 5.0 / 2.6 m | unchanged |
| Maximum crest height | 0.520 m | unchanged |
| Level crest length | 1.40 m | 0.80 m |
| Floor approach/exit lip | 45 mm | 5 mm |
| Max straight-segment grade | 14.7827° | <= 15.48° |
| Grade discontinuity at crest | abrupt 14.78° change | C1-eased 0.24 m transition, sampled every 40 mm |
| Concrete support | deck underlay to ends | continuous central 4.4 m support with tapered 0.3 m steel tips |
| Deck fasteners | side-mounted, possible 2 mm protrusion over road height | lowered to at least 23 mm below road height |
| Collider proposal | convex prism with four top profile vertices | **STATIC NONCONVEX** closed MeshCollider proposal, identical sampled visual top |

The **road top is one uninterrupted manifold steel shell**. The 0.24 m smoothstep-integrated slope ramps in/out continuously at the ground and crest. The central straight portions keep a bounded 15.48° maximum grade; the old 45 mm entrance lip is reduced to 5 mm. No props are placed on the drivable top. All side stiffeners/markers remain below the road height; side fasteners are now recessed. This reduces geometric snag risks but **does not establish tire physics PASS**.

**Collider limitation:** eased S-curve profile is non-convex. Proposed collider is a separate, closed, **static non-convex** mesh with an exact matching top and buried bottom; `convex=false` is required. It must not be assigned as a dynamic Rigidbody convex collider. Actual Unity contact, convex decomposition, tire clearance and performance: NOT_RUN.

## Coordinate system and provenance

- Blender metric scene: 1 unit = 1 metre, pivot at world (0,0,0), ground footprint centre. Blender +X across width, +Y forward travel, +Z up. GLB uses Y-up conversion. Unity import NOT_RUN.
- Static low-poly industrial sci-fi, 3 original PBR materials: `CONCRETE`, `STEEL`, `SAFETY_AMBER`. No rigs, animation, gameplay logic or textures.
- CC0-1.0 original authored procedural geometry; no competitor assets or third-party meshes.
- Owned paths only: `Tools/EnvironmentFactory/ENV02/ENV02-001/**`, own `.github/workflows/ci-env02-001.yml`. No changes to original claim, scenes, prefabs, ProjectSettings or canonical game.

## Reproduce and validate v002

```bash
blender --background --factory-startup --python Tools/EnvironmentFactory/ENV02/ENV02-001/generate_env02_001.py -- --out ./build/ENV02-001
python3 Tools/EnvironmentFactory/ENV02/ENV02-001/qa_env02_001.py ./build/ENV02-001
```

The generator creates `ENV02-001_v002.blend`, `ENV02-001_v002_visual.glb`, `ENV02-001_v002_collider_proposal.glb`, `ENV02-001_v002_view_3q.png`, `ENV02-001_v002_view_side.png`, and `ENV02-001_v002_blender_qa.json`. The independent validator creates `ENV02-001_v002_manifest.json` with SHA-256 of physical files and v001/v002 geometry metrics. GitHub Actions runs actual Blender headless, reopens the saved .blend, regenerates in a separate directory, compares GLB bytes, checks manifold/normal/triangle and matching collider profile, and uploads artifacts.

**Before PNGs (v001):** `ENV02-001_view_3q.png` and `ENV02-001_view_side.png` in the immutable original artifact linked above. **After PNGs (v002):** same two camera angles in the new artifact with `_v002` names. Review both pairs side-by-side. The original source SHA, physical BLEND/GLB and PNG SHA-256 are preserved in the v001 PR description and old manifest.

**Gates:** Producer Blender QA is not independent B01-60 review. B01-60 / B01-00 must explicitly accept the v002 SHA. No CODEX_ACK, no Unity/Windows/PhysX verification, no gameplay import or merge. Rules: [#276](https://github.com/C8LyNn/RoboRougelike/issues/276), [#407](https://github.com/C8LyNn/RoboRougelike/issues/407).
