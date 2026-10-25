# MODEL-01 / M01-024 — Mechaniczny harpun kotwiczący

**Catalog:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`.
**Claim:** `Docs/ModelFactory/claims/M01-024.json`, CREATE commit `5ce358c99f35b48f9fa374811bf28678c7e686cc`, 2026-10-25T10:49:32.828Z.
**Scope:** procedural visual asset only. No WPN mechanics, firing, projectile, damage, rope physics, scenes, prefabs or ProjectSettings changes.

## Art direction

Mechanical anchor harpoon with exposed right-side line drum, copper windings, riveted graphite housing, titanium guide, ivory ceramic muzzle and three backward-raked anchor claws. Seven materials: graphite, titanium, ceramic, copper, rubber, cyan emissive and amber warnings. The stationary line drum is decorative; only the harpoon spear translates.

## Coordinate contract

- 1 Blender unit = 1 metre. Geometry is authored Y-up, +Z-forward.
- `MOUNT_FRONT_SOCKET`: root at world (0,0,0), forward-facing vehicle mounting point.
- `HARPOON_TIP_EXTEND_Z`: child pivot at (0,0,0), translates +Z by 0.24 m and retracts; keys at frames 1,19,43,61,73 @60 FPS.
- Static muzzle ends at Z=0.887 m, moving shaft begins at Z=0.952 m; sampled AABB minimum separation is 0.065 m. Decorative cable is not a physically simulated rope.
- OBJ is static (retracted pose); FBX/GLB/BLEND contain Blender animation when Blender export succeeds.

## Generate and validate

```bash
python3 generate_m01_024.py --self-test
python3 qa_m01_024.py
blender --background --python-exit-code 1 --python generate_m01_024.py -- --repeat --out generated
python3 qa_m01_024.py --out generated --require-blender
```

The Python-only path produces a **real static OBJ/MTL** but does **not** produce FBX/GLB/BLEND. The Blender path exports actual binary formats and checks repeatability; CI uploads its outputs. No Unity import is asserted until tested.

## QA limitations

Static numerical tests verify watertight, oriented positive-volume meshes, deterministic OBJ generation, 73 animation samples and conservative moving/static AABB separation. They do not prove zero self-intersections among stationary parts or continuous triangle-level collision freedom. Blender execution, binary GLB translation-channel validation and Unity EditMode/PlayMode must be reported separately.
