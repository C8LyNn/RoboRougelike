# M05-018 — Przenośny reflektor roboczy / Portable Worklight

**Agent:** MODEL-05 · **Queue:** M05 · **Scope:** visual-only, no gameplay lighting or new sensor mechanics.

A rugged adjustable floodlight for the top socket of a modular robot. The visual design includes a graphite shock-isolated base, titanium U-yoke, ceramic shroud, copper hinge accents, nine warm emissive LED tiles, dark optical carrier, rear cooling fins, carry handle and green status strip. Nine separate materials distinguish the components even with emission disabled.

## Mount, orientation and animation

- Blender scale: **1 unit = 1 metre**; +Z up; optical forward **-Y**.
- Root mount pivot: `M05_018_MOUNT_TOP` at **(0, 0, 0)**.
- Tilt pivot: `M05_018_HEAD_TILT_PIVOT_X` at **(0, 0, 0.61)**, local **X axis**.
- Animation: frame 1 **-25°**, frame 40 **+35°**, frame 80 **-25°** at 30 FPS.
- Auxiliary empties: `OPTICAL_AIM_FORWARD`, `POWER_INPUT_REAR`, `SERVICE_CLEARANCE_TOP`.
- Static side-yoke inner face: X=±0.305 m; nominal moving housing X=±0.25 m.
- Moving head analytic envelope: local Y in [-0.29,0.31] m and Z in [-0.18,0.24] m.
- Conservative clearance is sampled at 121 angles and checks head/base and head/yoke only. Trunnion/bearing engagement is intentional. **Full triangle-triangle collision test NOT RUN.**

## Files

- `generate.py` — complete deterministic Blender `bpy` generator with named materials, closed primitives, applied bevels, explicit hinge animation, manifold check, GLB and BLEND export.
- `qa_blender.py` — independent repeat-build test in one Blender process, foreign-object preservation, orphan datablock check and GLB structure validation.
- `.github/workflows/ci-model-m05-018.yml` — Ubuntu Blender CI and 14-day downloadable binary artifacts.

## Run

```bash
blender -b -t 2 --factory-startup --python-exit-code 1 \
  --python Tools/ModelFactory/M05/M05-018/qa_blender.py \
  -- --output /tmp/m05-018
```

Expected outputs: `M05-018.glb`, `M05-018.blend`, `M05-018-qa.json`. FBX is deliberately **NOT RUN** unless separately verified. No Unity `.meta` or `Assets/` changes are made: Unity 6 import, URP visual check, EditMode and PlayMode are **NOT RUN** pending an actual Unity environment.

## Claim and isolation

Claim: `Docs/ModelFactory/claims/M05-018.json` on `feature/rrl-model-factory-queue-v1`.
Initial atomic claim commit: `3edd29cb0fe9aabfcd89e7b1f1c89444101ce801`.

Only `Tools/ModelFactory/M05/M05-018/` and the dedicated workflow are changed on the exclusive branch. No scenes, ProjectSettings, existing sensors, gameplay, other agent assets, `develop` or `main` are modified.
