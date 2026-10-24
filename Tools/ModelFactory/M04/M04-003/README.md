# M04-003 — Wentylator kanałowy dolny / Lower ducted fan

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. **Agent:** MODEL-04. **Claim:** `Docs/ModelFactory/claims/M04-003.json` on the control branch, commit `f5e2c523ab64a263ac6afd2632e716e2133bc7d5`, time `2026-10-24T12:52:54.936Z`.

## Design and mounting

- Visual-only underbody ducted fan, not a new hover/gameplay mechanic. One Unity unit is one metre. Unity **Y-up**, fan rotation axis **+Y**, airflow **-Y**.
- **Pivot (0,0,0)** at chassis underside mounting plane; all mesh below that plane. Rotor centre **(0,-0.302,0)**; seven swept ceramic blades; channel housing, structural cross, four pylons, hollow bearing, copper trim, eight cyan emissive arcs, amber warnings, lower grille.
- **51 closed components, 4146 vertices, 8124 triangles, 7 materials**. Rotor comprises nine moving components (hub, copper ring, seven blades). Stationary parts are parented to the mount.
- **Analytical clearances from generated vertices:** rotor-to-duct **45 mm radial**; bearing-to-shaft **16 mm radial**; rotor-to-lower-grille **96 mm axial**. Structural joints and decorative overlays intentionally overlap static supports.
- Blender animation: five quarter-turn keys across 60 frames at 60 FPS. Blender Z-up mapping preserves Unity Y-up when exporting. Animation is a visual reference, not physics/gameplay.

## Source and reproduction

The **complete original, readable Blender Python source** is preserved losslessly in `generate_m04_003.source.zlib.b64` with a SHA-256-verified executable Python launcher `generate_m04_003.py`. The launcher does not download anything. To extract an editable source file, run:

```sh
python Tools/ModelFactory/M04/M04-003/generate_m04_003.py --extract-source
```

This creates `generate_m04_003_readable.py` next to the launcher; the full source is also in the downloadable project ZIP. Regular execution:

```sh
python Tools/ModelFactory/M04/M04-003/generate_m04_003.py --self-test
python Tools/ModelFactory/M04/M04-003/generate_m04_003.py --out build/M04-003
python Tools/ModelFactory/M04/M04-003/qa_m04_003.py
blender --background --python Tools/ModelFactory/M04/M04-003/generate_m04_003.py -- --out build/M04-003
```

CPython writes **real static OBJ/MTL** and a manifest, plus **static GLB** if `trimesh` is installed. Blender additionally writes **animated FBX and GLB**. Unity 6 imports FBX/OBJ natively; GLB requires a compatible glTF importer.

## QA and limits

Self-test validates nondegenerate triangles, finite coordinates, watertight manifold edges, **consistent winding**, correct material references, moving-part count, and measured moving-part/guard clearances. Independent QA checks OBJ/MTL counts, GLB scene geometry and byte-for-byte repeatability. A dedicated CI workflow publishes static model artifacts.

**Executed locally:** Python source QA, actual static OBJ/MTL and GLB exports, GLB load using trimesh, deterministic second export. **NOT RUN:** Blender animated FBX/GLB, Unity import, Unity EditMode/PlayMode, visual inspection in Unity. This PR is **DRAFT** until visual/animation QA.

No shared scenes, ProjectSettings, prefabs, existing movement mechanics or other model IDs were modified.
