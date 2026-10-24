# M04-008 — Hover boczny stabilizujący / twin-lens side stabilizer

**Agent:** MODEL-04. **Katalog:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M04-008.json` on that same control branch, commit `7c4f876333d426afafcd8c5a7f001c7aebc8d0a9`.

## Design / mounting contract

Two independent, gimballed cyan hover lenses are aligned along the vehicle's longitudinal direction, with copper radiator rings, ceramic optical bezels, articulated titanium iris petals, exposed heatsinks, gunmetal channel shrouds and amber service LEDs. Seven named materials (Gunmetal, Titanium, Ceramic, Copper, Rubber, Cyan_Emitter, Amber_Status) give distinct readability even with emission disabled.

- **SIDE mounting, not top:** `RRL_M04_008_SIDE_MOUNT_ROOT` at **(0,0,0)** on the vehicle's side face. The mounting plate's back face lies exactly on **X=0**; **+X** points outward from the chassis, **+Y** runs longitudinally, **+Z** points up. All dimensions are in metres, 1 Blender unit = 1 Unity metre.
- **Bounds at rest:** X 0.000–0.400 m, Y −0.534–+0.534 m, Z −0.244–+0.244 m. The module does not project inward behind its mounting plane. To use on the opposite side of the chassis, rotate the prefab around the appropriate Unity local axis instead of moving its pivot.
- **Gimbal axes:** `FORE_Y_GIMBAL` at (0.310,−0.290,0), `AFT_Y_GIMBAL` at (0.310,+0.290,0), both rotating around their **local Y axes**. Two lens assemblies articulate in opposite phase at 0° → +16° → 0° → −16° → 0° over 81 frames / 30 FPS. Shrouds and side mounts remain rigid.
- **Clearance design:** hollow annular shrouds have an **inner radius 160 mm**; moving assemblies fit inside. Separate front lip, optical rings, cooling ribs and four angular iris petals per lens. Static housing contacts and bezel-to-lens layer contacts are engineered, not arbitrary intersecting pieces.
- **No gameplay/physics:** visual asset only. Rigidbody and collision primitives, flight physics, gameplay power, stability and cost remain the integrator's responsibility. Avoid a single non-convex MeshCollider on a dynamic Rigidbody.

## Generate and test

```bash
python3 Tools/ModelFactory/M04/M04-008/generate_m04_008.py --self-test
python3 Tools/ModelFactory/M04/M04-008/qa_m04_008.py
python3 Tools/ModelFactory/M04/M04-008/generate_m04_008.py --static-obj --out build/M04-008
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-008/generate_m04_008.py -- --out build/M04-008
python3 Tools/ModelFactory/M04/M04-008/qa_m04_008.py --check-glb build/M04-008/M04-008_side_hover.glb
```

The generator removes only its own `RRL_M04_008_SIDE_HOVER` Blender collection and orphaned namespaced mesh/action data. Repeated invocations are safe for unrelated scenes and collections. The standard-library fallback creates a **real static OBJ+MTL** and a manifest, not an illustration; OBJ does not preserve empties or animation. The Blender backend produces `.blend` and animated `.glb` files, then an independent GLB parser checks actual animation channels.

## QA limits

`validate()` verifies unique object names, seven materials, watertight components, consistent winding, positive volume, no degenerates, side mounting at X=0, rest envelope, two pivots and **65 sampled animation poses**. For every sampled pose it measures radial clearance of **every moving vertex** against the shroud bore, plus separation of the two complete emitter housings. The independent QA reads and checks the **actual exported OBJ** for topology, materials, geometry, bounds and rest clearance. It does **not** constitute arbitrary polygon–polygon collision detection, Blender viewport inspection or Unity playtesting. The generator reports the exact minimum sampled clearance.

**CI dependency:** Ubuntu Blender 4.0's glTF exporter imports `numpy`; the workflow explicitly installs `python3-numpy` alongside Blender. `--python-exit-code 1` ensures bpy export failures do not silently succeed.\n\n**Blender/GLB status:** only PASS when an actual CI run with Blender and independent GLB parsing completes. **Unity 6 import, EditMode, PlayMode, visual inspection:** NOT RUN until executed by an authorized Unity environment. Do not claim those from static geometry or Blender export.

**Isolation:** all source is under `Tools/ModelFactory/M04/M04-008/` plus a dedicated GitHub Actions workflow; no main scenes, ProjectSettings, existing gameplay or other model IDs are touched. This is a Draft PR against `develop`, never automatically merged.
