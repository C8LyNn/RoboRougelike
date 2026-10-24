# M05-003 — Skaner optyczny podwójny / Dual optical scanner

**Agent:** MODEL-05 (M05). **Claim:** `Docs/ModelFactory/claims/M05-003.json` on `feature/rrl-model-factory-queue-v1`, commit `8e7058cd59b2b885e6183547afd1d5410df3a018`, claimed `2026-10-24T12:49:24.267Z`.

## Model and scope

A visual-only **dual ruby-red optical scanner** with a chamfered graphite chassis, ivory ceramic trim, titanium twin optical barrels, ruby lens glass, independent decorative iris rotors, amber status indicators and a rear mounting collar. This is **not** a replacement for the existing SEN-001 scanner gameplay mechanism.

**Coordinates:** 1 Unity unit = 1 metre, Y up, lenses point **local +Z**. `MOUNT_FRONT` is at `(0,0,0)`, centre of the rear vehicle attachment face; chassis extends along +Z. Mounting plate is approximately 0.79 m wide, 0.35 m tall. Front bezel extends to z = 0.669 m. Optical axes are at x = ±0.215 m, y = 0.020 m. Rear collar has a genuine 86 mm central opening. Attach to a forward-facing vehicle socket.

**Animation:** `LENS_ROTOR_L` and `LENS_ROTOR_R` spin in opposite directions around their **local +Z** optical axes; keyframes at frames 1,16,31,46,61, 60 FPS. These are visual turntable keys only; no scanning or aiming code. The lens, iris and bezel have axial separation, and each iris has >15 mm radial clearance inside its barrel throughout the full 360-degree rotation. Source QA checks all five quarter-turn poses. Fixed static overlap between chassis and barrel roots is intentional.

**Materials:** nine distinct materials including opaque ruby optics and three emissive accents (ruby glass, crimson emitter and amber indicators). Opaque ruby material avoids Unity transparency-sorting artifacts; emission is a secondary accent, not the only shape cue.

## Reproducible generation

Python 3.11+ without Blender:

```sh
python3 Tools/ModelFactory/M05/M05-003/generate_m05_003.py --self-test
python3 Tools/ModelFactory/M05/M05-003/generate_m05_003.py --out Tools/ModelFactory/M05/M05-003/generated
python3 Tools/ModelFactory/M05/M05-003/qa_static.py --asset-dir Tools/ModelFactory/M05/M05-003/generated
```

This produces a real **static OBJ/MTL** plus JSON manifest; OBJ is directly importable by Unity without a glTF importer. The generated directory is populated by CI after passing QA and can be checked into this isolated feature branch.

Blender 4.2+ / 5.x:

```sh
blender --background --python Tools/ModelFactory/M05/M05-003/generate_m05_003.py -- --out build/M05-003
```

This additionally authors all Blender meshes/materials, builds the mount and two rotor parents, keys the two counter-rotating iris animations, and exports **FBX and animated GLB**. FBX is the intended native Unity animated import. A GLB importer is required for GLB in Unity. Repeated script runs remove and recreate only the dedicated `MODEL_M05_003` collection and its orphan meshes/actions.

## QA and release gate

The generator verifies closed-manifold triangles, positive volume, non-degenerate faces, finite coordinates, unique names, material assignments, exact iris count, model bounds, and analytic swept clearance for both rotors. `qa_static.py` independently parses the actual OBJ and MTL, checks face integrity and per-object manifold topology, validates emissive materials, and regenerates the static assets to compare SHA-256 hashes.

**Current restrictions:** Blender runtime and animated FBX/GLB export: **NOT RUN unless an actual Blender job executes**. Unity import/Play Mode, visual inspection, rig/animation import and vehicle clearance: **NOT RUN**. Passing Python QA does not authorize merging. This PR must remain Draft until those checks are performed. No scenes, gameplay scripts, ProjectSettings, main or develop are modified.
