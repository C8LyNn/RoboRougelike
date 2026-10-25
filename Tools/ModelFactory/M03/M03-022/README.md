# M03-022 — Zewnętrzna kratownica boczna

Catalog: `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. Agent: **MODEL-03**. Claim: `Docs/ModelFactory/claims/M03-022.json`. Scope: **visual modular side-frame attachment only**, no gameplay or shared Unity asset changes.

## Geometry and appearance

External triangular truss panel: 1.76 m longitudinal rails (X), 0.64 m nominal total height (Z), mounted outside the vehicle side at Y = -0.165 m. Eight alternating diagonals form a true zigzag of **triangular open cells**, rather than a flat grid texture. Two hollow graphite rails, two hollow titanium end posts, eight hollow titanium diagonals, nine copper nodal sleeves, four structural mounting standoffs, four mounting plates, four rubber isolators, four ceramic impact tiles, and four amber markers. Six distinct PBR materials: graphite, titanium, copper, ceramic, rubber, amber. Annular bores are actual geometry.

41 individually closed manifold meshes. Structural intersections **at beam welds, collar junctions and plate attachment faces are intentional**; diagonal spans are non-crossing. No moving components required by catalog. A full triangle-triangle intersection audit and manual render review remain separate acceptance tasks.

## Coordinates and mounting

Blender/Unity scale **1 unit = 1 metre**. Root pivot `M03-022_ROOT_PIVOT` at (0,0,0), chassis-side mounting plane Y=0; +Y points inboard. Four `M03-022_SNAP_01..04` empty locators at X=±0.73 m, Y=0, Z=±0.25 m, normal **+Y**. The truss extends outboard along -Y. Do not assume this convention fits a particular prefab until Unity verification.

## Generate and verify

```bash
blender --background --python-exit-code 1 --python Tools/ModelFactory/M03/M03-022/generate_m03_022.py -- --output /tmp/M03-022
blender --background --python-exit-code 1 --python Tools/ModelFactory/M03/M03-022/qa_blender_m03_022.py -- --output /tmp/M03-022
```

Exports `M03-022.blend`, `M03-022.glb`, `M03-022.fbx`; a dedicated GitHub Actions workflow validates and uploads them. QA checks all 41 meshes for watertight manifold edges, nondegenerate faces, positive signed volume, 6 materials, 4 sockets, diagonal topology, stable repeat generation, survival of foreign objects, and the **actual binary GLB**. No animation is exported.

## Unity 6 handoff

Import GLB/FBX into a **new isolated test asset**; inspect URP material conversion, smooth shading, +Y mounting orientation, scale, physics collision envelopes, snap alignment, and visibility at gameplay camera distance. Prefer simplified colliders built during integration, not per-triangle mesh collision for a moving vehicle. **Unity EditMode/PlayMode NOT RUN** until a genuine Unity runner and activation are available. No edits to `ProjectSettings`, shared scenes, or existing gameplay.
