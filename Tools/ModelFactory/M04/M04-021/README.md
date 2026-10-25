# M04-021 — Ster ogonowy przegubowy

MODEL-04 / RoboRougelike. **Visual-only** model for Unity 6; no gameplay, physics, scene, ProjectSettings or other agents' modules changed.

## Shape and mounting

Rear chassis plane **Y=0**; root at **(0,0,0)**; rearward **-Y**, vertical **+Z**. One Blender unit = one Unity metre. Two-piece swept silhouette: stationary armored forward fin and an aft steering blade on an exposed vertical hinge. Graphite body, satin titanium ribs, copper bracing, ivory ceramic trailing edge, elastomer gasket, cyan position indicators and amber warning lamps (**7 materials**). Four captive mount bolts, paired tail booms and exposed hinge races.

Hinge pivot **(0,-0.79,0)**, local **Z** axis. Rudder sweeps **-24° to +24°** through keys at frames **1,22,55,88,121**, 30 FPS. It is a *visual* animation; no force or steering behavior is implemented. Fixed forward fin ends at approximately Y=-0.70m; the rudder begins aft of the hinge, with a clearance margin checked over 97 steering poses.

## Generate and validate

```bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-021/generate_m04_021.py -- --out build/M04-021
python3 Tools/ModelFactory/M04/M04-021/qa_m04_021.py build/M04-021
```

Produces native `.blend`, animated `.glb` suitable for Unity glTF import through a compatible importer, and `M04-021_QA.json`. Import into Unity **NOT RUN** until an actual Unity environment with required activation is available. No unverified Unity compatibility claim.

Generator deletes existing scene objects and reconstructs a deterministic named model on each run. It checks mesh manifold edges, finite vertices, positive face areas and sampled panel-vs-fixed-fin clearance at 0.5° increments. Mechanical hinge/bearing contacts are intentional and excluded from the swept-panel clearance comparison. This is **not** exhaustive continuous triangle-triangle collision detection, Blender visual review, or Unity physics testing.

## Review gate

Check shaded appearance and glTF orientation in Unity; inspect hinge/bearing contact and extremal yaw in Blender, verify materials and prefab pivot. Keep PR in DRAFT until independent review. Do not merge automatically.
