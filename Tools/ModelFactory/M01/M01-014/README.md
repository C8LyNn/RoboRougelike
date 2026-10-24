# M01-014 — Elektromagnetyczny impulsator

**Source:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. This is a **visual-only** model, not a new weapon mechanic.

## Silhouette and assembly

A broad front mounting plate, reinforced rear electronic block, four separated heavy ferrite yokes, exposed copper induction coils, ceramic face insulators, side load rails and an axial cyan-emissive ferrite core. Seven PBR materials: graphite, titanium, ivory ceramic, copper, dark gasket, cyan signal and amber warning.

**Unity axes:** +Y up, +Z forward, +X right. **Scale:** 1 unit = 1 metre. **Mount:** `MOUNT_FRONT_SOCKET` at (0,0,0), flat mounting plane Z=0.

**Animated pivot:** `FERRITE_CORE_SLIDE_Z` at (0,0.36,0), child of the mount. The retained ferrite core moves from 0 to +0.18m along local Z and back over 61 frames at 30 FPS. No actual projectile, gameplay script or collision behaviour is included. The minimum annular bore radius is 0.152m; the moving core's maximum radius is 0.096m.

## Reproduction

From repository root:

```bash
python Tools/ModelFactory/M01/M01-014/generate_m01_014.py --self-test
python Tools/ModelFactory/M01/M01-014/generate_m01_014.py --out Tools/ModelFactory/M01/M01-014/generated
blender --background --python Tools/ModelFactory/M01/M01-014/generate_m01_014.py -- --out Tools/ModelFactory/M01/M01-014/generated --repeat-blender
```

Without Blender, Python generates actual static OBJ, MTL and JSON QA. With Blender 4+, the script also writes an animated GLB, FBX and editable BLEND. Repeated Blender runs clear the isolated scene and are checked for leaked objects/materials.

## QA limits

The script checks each component for watertight oriented triangle topology, positive signed volume and nondegenerate faces. The 61-frame swept broad-phase AABB test checks the translating core against fixed noncoaxial geometry. Concentric annular bores are checked with radial clearance instead of their necessarily overlapping AABBs. These are deterministic static checks, **not** a substitute for full triangle-triangle continuous collision detection or visual inspection. Unity import, EditMode, PlayMode and manual art review must be marked NOT RUN until executed.

## Isolation

All source and generated files are under `Tools/ModelFactory/M01/M01-014/`. No existing WPN-001..006 logic, scenes, prefabs, ProjectSettings, main or develop branch changes. Draft PR to develop only.
