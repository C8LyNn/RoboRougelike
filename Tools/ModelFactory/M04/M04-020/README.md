# M04-020 — Wysuwany booster manewrowy

Catalog: M04-020 (rear chassis mount, telescoping engine, piston rails). This is **only a procedural visual asset**; it does not add gameplay or alter Unity scenes.

## Geometry and attachment

- Blender coordinates: Z up; chassis rear mounting plane **Y=0**; exhaust points toward **-Y**. Unity glTF importer handles axis conversion. Unit scale is metres (1 Unity unit = 1 m).
- Root object: `M04-020_ROOT_MOUNT_Y0`, pivot at `(0,0,0)`. Attach this root to the chassis rear socket.
- Two paired external rail sets, a broad mounting plate, titanium slide rods, a sliding armored carriage, visible copper cooling fins, a double-wall ceramic nozzle, a six-vane rotor and cyan/amber indicators.
- Seven PBR materials; emission is only an accent, not the whole silhouette.
- Telescoping motion: carriage moves 0.28 m along local **-Y**, deploy/hold/retract in frames 1–121 at 30 fps. Rotor pivots on local Y and spins twice during the same 121 frames.
- Rail lateral inner boundary is at |X|≈0.365 m; all moving geometry stays within |X|≤0.321 m except exhaust bell at ≤0.229 m. The conservative lateral clearance to rails is at least 44 mm for the sliding piston. Nonmoving rail components remain outside the swept path.
- Intentional internal contact/overlap occurs between connected engine parts (e.g. structural bands and housing) and is not a collision defect. External static-vs-moving clearance is sampled through the full animation.

## Reproducible generation

```bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-020/generate_m04_020.py -- --out build/M04-020
python3 Tools/ModelFactory/M04/M04-020/qa_m04_020.py build/M04-020
```

Expected generated artifacts: `M04-020.blend`, `M04-020.glb` (with translation and rotation animation), `M04-020_QA.json`. The script clears the scene on each run, preventing accumulation.

## QA scope

Generator checks mesh presence, finite coordinates, material count, and 25 sampled frames of AABB separation between static and moving parts. Independent validator inspects binary GLB structure, material and mesh counts, and translation/rotation channels. **AABB checks are conservative discrete samples, not continuous triangle collision detection.** Unity EditMode, PlayMode, import, visuals and physical gameplay require a Unity environment and are NOT RUN unless separately evidenced.

No FBX is required: animated GLB is the canonical interchange format. No changes to `Assets/`, main scenes, `ProjectSettings/`, or existing modules.
