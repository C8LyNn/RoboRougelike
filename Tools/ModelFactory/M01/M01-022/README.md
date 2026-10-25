# MODEL-01 / M01-022 — Samonaprowadzający zasobnik dronów

Visual-only, independent 3D asset for Unity 6 RoboRougelike, strictly from `Docs/ModelFactory/CATALOG_V1.md` on the control branch. No homing, launching, combat logic or changes to existing WPN systems. The six drones are **stowed visual geometry**, not AI agents.

## Design and transforms

A wide low-profile carbon housing with a **2×3 array of hexagonal launch mouths**, six ivory ceramic drone hulls with cyan sensors, and **six individually hinged armored doors**. Seven PBR materials: carbon, titanium, ivory ceramic, copper, rubber, cyan emissive signal, amber warning. Geometric edges are chamfered in the procedural source. Units are metres, native coordinate system **Y-up / +Z-forward**; `MOUNT_TOP_SOCKET` at `(0,0,0)` is the root mount. Unity FBX export explicitly specifies `axis_up='Y'`, `axis_forward='-Z'`; verify handedness and local forward in the importer before use.

Each door has a named `DOOR_01_HINGE_X` … `DOOR_06_HINGE_X` pivot at the top edge. A 73-frame, 60-fps cycle swings all doors from 0° to **−68° around local X**, pauses, then closes. The hinges are intentionally at the door's upper edge; the sweep is outwards (+Z), away from the chassis. This is a display animation; gameplay animation triggers are not included.

## Build and verification

```sh
python3 Tools/ModelFactory/M01/M01-022/qa_m01_022.py
blender --background --python-exit-code 1 --python Tools/ModelFactory/M01/M01-022/generate_m01_022.py -- --repeat --out Tools/ModelFactory/M01/M01-022/generated
python3 Tools/ModelFactory/M01/M01-022/qa_m01_022.py --out Tools/ModelFactory/M01/M01-022/generated --require-blender
```

Without Blender, generator produces **only static OBJ, MTL, QA JSON**, never claims binary exports. With Blender, writes animated GLB, FBX, BLEND as well. Independent QA parses the OBJ mesh, validates all six cell/door/drone sets, compares exact deterministic geometry, checks the binary GLB header and animation channels, verifies FBX/BLEND magic and checks repeatability. Geometry is individually watertight with consistent face winding and positive signed volume. Conservative sampled AABB checks cover 73 poses and door-door separation; **not** continuous triangle-triangle collision detection. Intentional static structure joins are not subject to inter-part penetration tests.

## Status and restrictions

The script is isolated under `Tools/ModelFactory/M01/M01-022/`. GitHub Actions performs real Blender generation and stores artifact files; Unity import, EditMode, PlayMode, manual visual evaluation and gameplay integration remain **NOT RUN** until separately executed. No modifications to `main`, `develop`, shared scenes, prefabs, `ProjectSettings`, or WPN mechanics. Keep the PR in DRAFT pending Unity validation.