# M04-017 — Pierścień teleportacyjny wizualny

**Queue:** M04, visual-only teleport ring. **Mount:** top chassis socket, local mount origin `(0,0,0)`, mounting face `Y=0`. **Scale:** 1 Blender unit = 1 Unity metre; +Y is up, +Z is vehicle forward.

The asset is a distinctive eight-piece armored annulus with separated graphite/titanium/cyan concentric bands, copper index tabs, amber status indicators, a recessed optical projector and two rear support rails. Seven physically distinct materials are used. It is **visual only**; no teleportation, locomotion or game logic is implemented.

## Animation and pivots

- `SPIN_PIVOT_Z_RING_CENTER`: located at `(0,1.22,0)` m, full 360° rotation around local Z, keyed in quarter-turns (frames 1–121, 30 FPS).
- `SEGMENT_PIVOT_01..08`: local radial translation from 0 to 0.075 m, keyframes 1,21,61,101,121. Each pivot owns its own five visible meshes.
- Angular sector gaps: 5° each. Outer annulus radius 0.866 m; opened outer extent 0.941 m. Stationary rear supports occupy positive Z behind the rotating annulus.

The geometry has manifold annular-sector shells, proper end caps and outward-facing winding. Curved walls are smoothed while caps remain hard-edged. The generator clears the active Blender scene before rebuilding, preventing accumulation on repeated runs.

## Generate and validate

```bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-017/generate_m04_017.py -- --out build/M04-017
python3 Tools/ModelFactory/M04/M04-017/qa_m04_017.py --out build/M04-017
```

The generator writes a native `.blend`, animated `.glb`, optional static `.obj/.mtl` and JSON manifest. Independent QA checks GLB binary integrity, eight sector translation channels, main-ring rotation channel, materials, geometry counts and optional OBJ indices. Blender runtime validation checks watertight mesh edge usage and nondegenerate faces. The 5° segment gaps, separated concentric radial bands and Z-separated support rails are checked conservatively; these are **not** a substitute for full per-triangle swept collision testing.

## Scope and integration

Do not merge automatically. A reviewer may import the generated GLB into Unity 6, confirm axis conventions, physically mount the base on a chassis top socket, inspect materials and run EditMode/PlayMode tests. The model does not modify `Assets`, shared scenes, `ProjectSettings`, physics or existing modules. Missing Unity activation means **NOT RUN**, never PASS. Visual review remains NOT RUN until performed.

**Claim:** `Docs/ModelFactory/claims/M04-017.json` on `feature/rrl-model-factory-queue-v1`; claim commit `055807e745de8e597c67249180ec12434793a410`.
