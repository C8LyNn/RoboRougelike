# M02-023 — Łańcuchowy napęd boczny (MODEL-02)

Claim: `Docs/ModelFactory/claims/M02-023.json` on `feature/rrl-model-factory-queue-v1`. **CREATE commit:** `d8aea38fe3b77f237f89febf6cb9ae8c85185291` at 2026-10-25T07:50:45.460Z.

## Model and contract

Visual, side-mounted roller-chain speed-up drive with **24-tooth input sprocket**, **16-tooth output sprocket**, **48 rigid chain links**, paired side plates, bronze rollers, open eight-spoke sprockets, frame attachment and service details. The two sprockets rotate in the **same direction** with angular speed ratio **2:3** (input:output). No gameplay, force transmission, Rigidbody, or Unity scripts are added.

- **Scale:** 1 Blender unit = 1 metre.
- **Pivot:** `MOUNT_SIDE_SOCKET_UNITY_1M` at world origin `(0,0,0)`; side-mount face near Y=0.
- **Coordinates:** Blender +X horizontal, +Y shaft axis, +Z vertical. Chain plane Y=-0.240 m. Sprocket centreline Z=-0.205 m.
- **Kinematics:** 121 frames at 30 FPS. Chain travels one complete closed loop; the 24T wheel turns twice, 16T wheel three times. Each link has animated position and orientation along analytically computed tangents and pitch-circle arcs. All 50 moving groups are assigned to a single named NLA track for a synchronized glTF cycle.
- **Visual separation:** radial roller-to-tooth-tip envelope >=5 mm; plates sit outboard of the sprocket plane; chain plates have swept 2D oriented-box SAT collision checks.
- **Materials:** eight PBR materials: forged graphite, alloy, bronze, safety orange, rubber, ceramic, hardened steel, emissive service indicator. Emission is a minor indicator, not the main appearance.

## Files

- `kinematics_m02_023.py`: Blender-independent pitch, centre distance, path, link poses, and 121-frame swept SAT checks.
- `generate_m02_023.py`: complete `bpy` procedural generator, reentrant scene reset, topology QA, BLEND and animated GLB export.
- `qa_m02_023.py`: independent source/kinematic QA and binary glTF animation validation.
- `generated/`: created by Blender workflow; binaries uploaded as GitHub Actions artifacts rather than committed to the PR.

## Execute

```bash
python3 Tools/ModelFactory/M02/M02-023/qa_m02_023.py --math-only
blender -b -noaudio --python Tools/ModelFactory/M02/M02-023/generate_m02_023.py
python3 Tools/ModelFactory/M02/M02-023/qa_m02_023.py
```

The generated GLB is a **Unity-import candidate**. Unity import, runtime physics, mesh-mesh collision, visual inspection and FBX export are **NOT RUN** until independently verified. Do not merge the Draft PR without Unity 6 and visual QA. Swept SAT is a simplified 2D clearance test; tooth profiles are stylised with a 5 mm roller gap and do not simulate loaded involute tooth contact.
