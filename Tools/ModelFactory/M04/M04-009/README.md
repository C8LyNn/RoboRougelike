# M04-009 — Stabilizator żyroskopowy (3 osie)

**Agent:** MODEL-04. **Queue:** M04, `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M04-009.json` on the control branch, GitHub CREATE commit `9bfeb66e858bc7e3288ad329dda7af27e4e28912` (2026-10-24T18:48:15Z). This is a **new visual model**, not gameplay stabilization code.

## Geometry and artistic direction

A compact internal gyroscope suspended inside a four-pylon graphite/titanium safety cage. Three distinct, concentric **spherical great-circle gimbals** form an articulated X/Y/Z nesting: outer titanium/copper Z ring, intermediate graphite/copper Y ring and inner titanium/ivory X ring. The inner ring carries a blue glass spherical rotor with an emissive central lumen and four copper spokes. Peripheral cyan electromagnetic nodes, copper trims, an eight-material palette, amber corner status lights and visible cage fasteners improve readability at typical third-person camera distances.

**Mounting:** The local root `RRL_M04_009_FLOOR_MOUNT_ROOT` is **(0,0,0)** on the *bottom centre of the internal chassis floor*. The underside of `MOUNT_FLOOR` lies at **Z=0**; **+Z** points up, **X/Y** span the floor. No side or top mount is substituted. 1 Blender unit = 1 Unity metre. Gimbal centre is **(0,0,0.380 m)**. This is an internal module, not an exposed weapon or thrust source.

**Animation:** `OUTER_Z_PIVOT` (yaw, local Z, ±38°), child `MIDDLE_Y_PIVOT` (pitch, local Y, ±28°), child `INNER_X_PIVOT` (roll, local X, 360°/cycle). 121 frames at 30 FPS; rotation keyframes every 10 frames. Moving parts are parented to their own pivot, with mesh vertices expressed in each pivot's local coordinate system. Exported GLB retains the 3 rotation channels; static OBJ is a rest-pose preview only.

**Clearance design:** Ring assemblies occupy strictly disjoint spherical radial shells, including trim and emitter studs. Their radial separation is invariant under arbitrary combinations of rotations, so the three nested gimbals cannot strike each other at any angle. Stationary safety cage boxes are kept outside the full gyroscopic swept volume. Source QA checks all 121 animation poses against each actual fixed cage box and reports the minimum clearance. Welded/decorative same-group overlaps (copper ring inlay, central rotor spokes and glass core) are intentional; this is not a general triangle/triangle collision solver or Unity physics validation.

## Commands

```bash
python3 Tools/ModelFactory/M04/M04-009/generate_m04_009.py --self-test
python3 Tools/ModelFactory/M04/M04-009/qa_m04_009.py
python3 Tools/ModelFactory/M04/M04-009/generate_m04_009.py --static-obj --out build/M04-009
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-009/generate_m04_009.py -- --out build/M04-009
python3 Tools/ModelFactory/M04/M04-009/qa_m04_009.py --check-glb build/M04-009/M04-009_gyroscope.glb
```

The Blender generator clears only its own namespaced collection and unused namespaced mesh/action datablocks. It is safe to rerun without accumulating duplicate module objects. Blender exports `.blend` and animated `.glb`. Standard-library mode exports a real static `.obj` and `.mtl`, plus JSON QA manifest. The independent QA parses actual OBJ triangles and GLB animation targets, and verifies byte-identical repeated static generation.

## QA boundaries and integration

**Unity 6 import, EditMode, PlayMode, physics, runtime performance and manual visual inspection are NOT RUN** unless an authorized Unity test actually executes. This module is not a Rigidbody, torque controller, collision mesh, vehicle ability or economy item; these belong to game integration. Do not place the complete high-poly visual mesh in a non-convex MeshCollider on a moving Rigidbody; construct simple convex colliders if needed.

Files are isolated to `Tools/ModelFactory/M04/M04-009/` and `.github/workflows/ci-m04-009.yml`. No edits to scenes, gameplay modules, prefabs, ProjectSettings or other agents' assets. PR is Draft against `develop`; no merge.
