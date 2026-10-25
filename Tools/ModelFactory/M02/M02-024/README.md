# M02-024 — Koło wielokierunkowe z rolkami (mecanum)

**MODEL-02 / visual asset only.** This package does not replace or alter the existing `MOV005OmniDrive` gameplay controller.

## Geometry and mount

- Scale: **1 Blender unit = 1 metre**; Y is the side axle, wheel rolls in the XZ plane.
- Root mounting datum: `MOUNT_SIDE_AXLE_ORIGIN_1M` at **(0,0,0)**, the centre of the fixed axle. The chassis mounting flange is at Y = -0.245 m.
- Wheel outer envelope approximately **0.808 m diameter**, rotor rings radius 0.280 m, 12 barrel rollers at radius 0.355 m.
- Each roller is tilted **45 degrees** in the tangential/axial plane. Roller diameter 98 mm, barrel length 165 mm; a bronze bearing/endcap pair and radial carrier arms connect each roller to the wheel rotor.
- Eight PBR materials: graphite steel, brushed alloy, rubber, bronze, orange, ceramic, hardened steel and cyan service accent.
- Fixed shaft and mounting plate do not rotate. `WHEEL_SPIN_Y` rotates about **Blender -Y** for positive path angle; 12 `ROLLER_SPIN_NN_Z` objects rotate about their respective 45-degree local Z spindle axes.
- A 121-frame synchronized NLA cycle named `M02_024_MECANUM_CYCLE` demonstrates wheel rotation and independent roller rotation. Passive roller motion is illustrative; in-game speeds must be driven by the physics controller.

## Generate / verify

```sh
python3 Tools/ModelFactory/M02/M02-024/qa_m02_024.py --math-only
blender -b -noaudio --python Tools/ModelFactory/M02/M02-024/generate_m02_024.py
python3 Tools/ModelFactory/M02/M02-024/qa_m02_024.py
```

Outputs (after actual execution) under `generated/`: editable `M02-024.blend`, animated `M02-024_animated.glb`, `M02-024_qa.json`. CI uploads them as an Actions artifact, not to a shared Unity scene.

## QA limitations and integration

Analytical QA samples **121 frames × 66 unique roller pairs = 7986** capsule-to-capsule distance tests, checks the rim clearance and 45-degree roller axes. Capsule bounds are conservative for the rubber barrel but do **not** include deliberately contacting bearings, internal axle, fasteners or carrier supports. They do not constitute a full triangle-triangle collision test or loaded bearing simulation.

GLB validation independently parses actual glTF 2.0 binary animation accessors and checks 13 distinct moving groups with nonconstant quaternion samples. Blender CI results must be checked before claiming PASS. Unity import, EditMode, PlayMode, physical load testing, visual inspection and FBX export are **NOT RUN** unless independently executed and evidenced. Do not merge this Draft PR before integration review. No modifications to gameplay, shared scenes, ProjectSettings, main/develop or other agents' packages.
