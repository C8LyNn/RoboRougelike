# M03-014 — Płoza podwozia ślizgowego

**Agent:** MODEL-03 / M03. **Catalog:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M03-014.json`, CREATE commit `53524b2e0d86079b8634f9fe50fcaa90ec050b70`, 2026-10-24T23:48:34.667Z. Standalone visual model for Unity 6 RoboRougelike; no gameplay or shared scene changes.

## Geometry and mount specification

A 2.00 m long ceramic underbody skid with a continuous raised nose and tail, graphite load spine, two lateral steel rails, two ceramic side-wear strips, four elastomer isolation pads, four copper annular surface sockets and four amber orientation indicators. **18 independently closed meshes; five materials**: graphite steel, ivory ceramic, copper, rubber and amber. No animation (catalog says `brak`).

Source coordinates: **1 unit = 1 metre**, +X right, +Y up, +Z vehicle forward. Pivot at **(0,0,0)**, the top plane of the four sockets. All geometry is below the mounting plane; lowest belly point Y=-0.242 m. Four SNAP empties `SNAP_BOTTOM_{L,R}_{Aft,Fore}` at X=±0.19095, Y=0, Z=±0.5; outward attachment axis -Y. The hollow copper rings are **surface sockets**, not through-bores in the rubber pads or steel backbone. They are not to be misrepresented as through-holes.

Layer interfaces are face-to-face; there are no intentional positive-volume overlaps. The independent OBJ QA conservatively checks AABB volume separation for all 153 mesh pairs. The long belly's rising ends are actual geometry, not a normal map.

## Files and use

- `generate_m03_014.py`: deterministic dependency-free OBJ/MTL + JSON manifest, with Blender `bpy` generation and GLB/FBX/BLEND export when Blender is available.
- `qa_m03_014.py`: reads generated OBJ independently; tests watertight edges, positive winding/volume, mount positions, 5 materials, AABB separation and byte-for-byte reproducibility.
- `.github/workflows/ci-m03-014.yml`: isolated static QA and Blender exporter with downloadable build artifacts.

Run locally:

```bash
python3 Tools/ModelFactory/M03/M03-014/qa_m03_014.py
python3 Tools/ModelFactory/M03/M03-014/generate_m03_014.py --out build/M03-014
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-014/generate_m03_014.py -- --out build/M03-014
```

**Verification:** dependency-free static generator and QA were run locally: PASS, 18 closed oriented meshes, 5 materials, 4 SNAP ports, 153 pairwise AABB checks, deterministic OBJ/MTL/manifest. Blender export and Unity 6 import/PlayMode are **NOT RUN** until confirmed by real CI/engine execution. Visual shading and actual vehicle attachment must be inspected before integration. Keep PR DRAFT.
