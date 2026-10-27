# ENV02-001 — rampa terenowa / terrain ramp (concept asset)

**Scope:** One original, static low-poly industrial sci-fi obstacle-course ramp. Visual art pilot only; **no game integration**, no gameplay logic, no vehicle modification, no claim on M01–M05. Status: `MANUAL_ART_REVIEW_PENDING` until the owner reviews the actual exported asset.

## Dimensions and axis convention

- Blender scene: **1 Blender unit = 1 metre**, metric, scale 1.0. Origin/pivot `(0,0,0)` at ground level in the centre of the ramp footprint.
- Blender **+X** right across the width, **+Y** forward along the vehicle path, **+Z** up. glTF uses Y-up internally; Blender exporter handles the conversion. Unity orientation/scale **NOT TESTED**.
- Nominal drivable surface: **5.0 m long × 2.6 m wide**. Low deck tip **0.045 m** above the floor, crest **0.520 m**. Approach: Y -2.50 to -0.70 m; flat crest Y -0.70 to +0.70 m; exit Y +0.70 to +2.50 m. Both slopes **~14.8°**. Deck thickness **0.030 m**. Concrete foundation width **2.45 m**.
- Three completely planar, uninterrupted steel drive faces: approach, plateau and exit. All rivets, side stiffeners and amber markings are on the **outside** of the 2.6 m drive envelope, not across the rolling surface.
- Exactly **three simple materials**: `CONCRETE`, `STEEL`, `SAFETY_AMBER`. No external textures, no animation, no rig, no map generation.

## Proposed collision (NOT imported into game)

`COLLIDER_PROPOSAL_RAMP` is a single closed convex prism, matching the top steel deck profile (three drivable planes). It is visible as a wireframe helper in the editable BLEND, hidden from preview renders and **not part of the visual GLB**. A separate `ENV02-001_collider_proposal.glb` allows HQ to evaluate the geometry. Proposed **convex MeshCollider** or three planar convex box wedges for later review; no physics verification or Unity import has occurred. Real tire contact, edge clearance and penetration are **NOT_RUN**.

## Reproducible build

Requires Blender with Python `bpy` and its glTF exporter. From the repository root:

```bash
blender --background --factory-startup --python Tools/EnvironmentFactory/ENV02/ENV02-001/generate_env02_001.py -- --out ./build/ENV02-001
python3 Tools/EnvironmentFactory/ENV02/ENV02-001/qa_env02_001.py ./build/ENV02-001
```

Expected physical outputs: `ENV02-001.blend`, `ENV02-001_visual.glb`, `ENV02-001_collider_proposal.glb`, `ENV02-001_view_3q.png`, `ENV02-001_view_side.png`, `ENV02-001_blender_qa.json`, `ENV02-001_manifest.json`. GitHub Actions uploads these as one evidence artifact. Output files are only considered **real** when the build job has actually succeeded and the artifact exists; source presence alone does not constitute export.

## QA and review gate

Blender-native generator checks positive closed-manifold volumes, degenerate faces, positive signed normals, visual mesh transforms, metric scale, exactly three materials, static/no animation and **200–1500** rendered triangles. Independent Python validator parses the actual GLB binary and checks geometry counts, materials, collider isolation, Blender magic, PNG dimensions and SHA256 of physical outputs. The two PNGs are Workbench camera views, **not** Unity/URP renders.

**NOT_RUN:** Unity 6 importer, EditMode, PlayMode, Windows build, physics/collision response, URP materials, manual artistic review. **NO MERGE / NO INTEGRATION** until separate HQ authorization. This is not a `CODEX_READY` gameplay package.

## Authorship / licensing

All geometry, proportions, Blender source, materials and preview setup are **original procedural authored work for this pilot**, not copied or extracted from any game or competitor. No external mesh, texture, font or reference image is embedded. The authored asset and source are offered under **CC0-1.0** (public-domain dedication) by the producer for this project; Blender's own software and the GitHub Actions environment retain their respective licenses. If project governance requires a different contribution license, HQ should review before integration.

## Provenance

- Control: [`#287`](https://github.com/C8LyNn/RoboRougelike/issues/287) and [`#276`](https://github.com/C8LyNn/RoboRougelike/issues/276).
- Isolated source: `Tools/EnvironmentFactory/ENV02/ENV02-001/**` and its dedicated `.github/workflows/ci-env02-001.yml` only, plus single non-overwriting claim `Docs/EnvironmentFactory/claims/ENV02-001.json`.
- Claim created **before** any production. The PR remains **Draft** and targets `develop`; do not merge or copy into canonical Unity scenes/prefabs.
