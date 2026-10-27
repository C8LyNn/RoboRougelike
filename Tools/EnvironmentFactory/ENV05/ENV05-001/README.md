# ENV05-001 — Słupek świetlny / Industrial warning-light bollard

**One-time experimental ENV asset.** Status is `ASSET_REVIEW_PENDING` **only if** Blender and independent export QA actually pass. This is not an integrated Unity package. No further ENV05 models are authorized.

## Art direction and scope

Original static low-poly industrial sci-fi roadside marker for obstacle courses / arenas. One tapered graphite housing, steel corner ribs, bolted base, single amber warning beacon and top visor. No third-party geometry, textures, competitor models, lighting logic, AI, power system, gameplay scripts, animations or map generator. Exactly **three** materials: graphite, galvanized steel, amber with restrained emissive shading.

## Physical conventions

- Blender: **1 unit = 1 metre**; +Z up; -Y front (service hatch); +X right.
- Root pivot `ENV05_001_ROOT_PIVOT_GROUND` at **(0,0,0)**, centred at the bottom contact plane.
- Intended dimensions: **0.32 × 0.32 m** base footprint, about **0.874 m** height; measured Blender AABB in QA report.
- Exported glTF 2.0 `.glb`: Y-up after Blender export axis conversion. Importer coordinate transforms must be inspected before any Unity handoff.
- Suggested *future* collider (not added to game): single BoxCollider **0.32 × 0.32 × 0.88 m**, centre **(0,0,0.44)** in Blender axes.
- Triangle budget: **200–1500**, checked on real Blender geometry and independently on the exported GLB.
- Rig/animation: `NOT_APPLICABLE`; Unity 6, URP, Windows, PlayMode: `NOT_RUN`.

## Reproduce and verify

```sh
blender -b -t 2 --python-exit-code 1 --python Tools/EnvironmentFactory/ENV05/ENV05-001/generate_env05_001.py -- --out /tmp/env05-001 --self-test
python3 Tools/EnvironmentFactory/ENV05/ENV05-001/validate_env05_001.py /tmp/env05-001
```

Expected **actual** files only after successful execution: `ENV05-001_light_bollard.blend`, `ENV05-001_light_bollard.glb`, `ENV05-001_front_three_quarter.png`, `ENV05-001_rear_three_quarter.png`, `ENV05-001_qa.json`, `ENV05-001_export_validation.json`. The dedicated GitHub Actions workflow uploads physical files as a downloadable artifact with 14-day retention; this README does not assert that CI has passed.

Blender self-test regenerates twice in one scene, checks preservation of an unrelated sentinel object, triangle/material budget, closed manifold geometry, nondegenerate faces, base pivot and bounds. The independent validator checks real GLB structure/triangle count/material count, BLEND and PNG file signatures and SHA-256 hashes. Two orthographic previews are rendered with Blender Cycles on CPU.

## Original authorship / license

Created specifically for RoboRougelike ENV05-001 on 2026-10-27, using authored procedural geometry without external meshes or textures. Source, asset and generated outputs: **CC0-1.0** public-domain dedication. No competitor asset copied.

## Manual acceptance gate

After binary QA, HQ must review two views for silhouette/readability from gameplay distance, proportions, color and environment suitability. **No integration without separate HQ authorization.** Do not merge the Draft PR or modify any gameplay branch. This pilot must not be repeated; report `NO_WORK_AWAITING_MANUAL_ART_REVIEW` after completion.
