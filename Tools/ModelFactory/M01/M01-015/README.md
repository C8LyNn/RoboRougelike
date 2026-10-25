# M01-015 — Dwukierunkowa wieżyczka strzelecka

**Catalog:** `feature/rrl-model-factory-queue-v1:Docs/ModelFactory/CATALOG_V1.md` — exclusively M01-015.
**Agent:** MODEL-01 (M01). **Claim:** `Docs/ModelFactory/claims/M01-015.json` on control branch, CREATE commit `a81ba37083bf97999d40a14e1aa8c52e1a9fbba9` (2026-10-25T00:50:15Z).

## Geometry and coordinate system

A low-profile V-shaped graphite/titanium turret chassis supports two independently yawing weapon arms. Each arm has a hollow metallic barrel, ceramic segmented guards, copper cooling fins and cyan-emissive muzzle rings. Seven material definitions. No WPN gameplay code.

- **Unity:** Y-up, +Z-forward, metres (1 unit = 1 metre).
- **Mount:** `MOUNT_TOP_SOCKET` at (0,0,0), underside of turret plate.
- **Left yaw pivot:** `ARM_L_YAW` at (-0.25,0,0.26); right pivot at (+0.25,0,0.26).
- **Yaw:** mirrored about local +Y, from 8° to 32° outward; rest 16°.
- **Animation:** 73 frames at 30 FPS; keyframes 1/19/37/55/73.
- **Physical bearing:** moving 52mm-radius pivot shaft inside 69mm-radius annular bore; nominal radial clearance 17mm.

## Build

```bash
python Tools/ModelFactory/M01/M01-015/generate_m01_015.py --self-test
python Tools/ModelFactory/M01/M01-015/generate_m01_015.py --out Tools/ModelFactory/M01/M01-015/generated
python Tools/ModelFactory/M01/M01-015/qa_m01_015.py --out Tools/ModelFactory/M01/M01-015/generated
blender --background --python Tools/ModelFactory/M01/M01-015/generate_m01_015.py -- --out Tools/ModelFactory/M01/M01-015/generated --repeat-blender
python Tools/ModelFactory/M01/M01-015/qa_m01_015.py --out Tools/ModelFactory/M01/M01-015/generated --require-blender
```

Plain Python exports **static OBJ/MTL**; Blender additionally exports animated **GLB, FBX and BLEND**. The GitHub Actions workflow executes Blender if available, independently validates the files, uploads an artifact and attempts to publish generated files only on this isolated branch.

## QA limitations

- Closed oriented manifold and positive-volume checks run on individual mesh components; distinct components may deliberately meet at mount joints.
- 73-pose AABB sweeps verify moving-vs-fixed and left-vs-right separation; exact annular shaft/bearing gap is checked analytically. This is **not continuous collision detection**, nor a Unity physics test.
- Import into Unity 6, EditMode/PlayMode, and manual visual assessment remain **NOT RUN** until separately performed.
- The model is visual-only. No changes to existing WPN-001..006, scenes, prefabs, `ProjectSettings`, `main` or `develop`.
