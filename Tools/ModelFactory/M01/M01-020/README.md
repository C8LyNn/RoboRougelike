# MODEL-01 / M01-020 — Krótki emitujący falę rezonator

Source of truth: `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`.
Claim: `Docs/ModelFactory/claims/M01-020.json`, CREATE commit `188402cdd032b3664ccf9c67101f6300883cd29b`, timestamp `2026-10-25T06:48:02Z`.

## Design

Compact short-range visual resonator with a thick graphite power housing, exposed copper induction coils, six sculpted ceramic waveguide horns, amber maintenance markers, a cyan aperture, and three suspended six-lobed sinusoidal wavefront rings. Seven distinct materials including physically based metallic and emissive accents. This is a **visual asset only**: no projectile, targeting, damage, or WPN mechanics.

## Coordinate and motion contract

- 1 unit = 1 metre, Y-up and +Z-forward authored coordinates.
- `MOUNT_FRONT_SOCKET` pivot at `(0,0,0)` on the rear mounting face.
- `WAVE_RING_VIBRATE_Z` parent at `(0,0,1.065)` moves three separate wavy rings rigidly along local Z.
- Motion: 4 cycles of ±24 mm over 73 frames at 60 FPS. No deforming or penetrating the housing.
- Sampled collision QA: 73 frames × 3 moving parts × 46 fixed parts = 10,074 conservative AABB pair checks, no overlap; minimum separating axis 0.121067 m. This is not continuous triangle-triangle collision detection.
- Static OBJ/MTL do not carry animation; animated GLB/FBX/BLEND are exported only by Blender.

## Build

```sh
blender --background --python-exit-code 1 --python Tools/ModelFactory/M01/M01-020/generate_m01_020.py -- --out Tools/ModelFactory/M01/M01-020/generated --repeat
python3 Tools/ModelFactory/M01/M01-020/qa_m01_020.py --out Tools/ModelFactory/M01/M01-020/generated --require-blender
```

Without Blender, the generator can create **only static OBJ, MTL, and QA JSON**. Python-only checks validate watertightness, positive volume, triangle winding, nondegenerate faces, deterministic static export, material assignments, and sampled moving/static clearance.

Unity 6 import, EditMode/PlayMode tests, and manual visual inspection remain **NOT RUN** unless evidenced separately.

## Isolation

Only this model's own directory and its dedicated workflow are changed on an exclusive feature branch based on `develop`. Separate DRAFT PR; no modifications to WPN-001..006, shared scenes, prefabs, ProjectSettings, or other agents' assets.
