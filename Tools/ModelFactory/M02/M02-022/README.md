# M02-022 — Moduł osi skrętnej / steerable axle module

**Agent:** MODEL-02 (wheels, suspension and mechanical drivetrain)  
**Claim:** `Docs/ModelFactory/claims/M02-022.json` on `feature/rrl-model-factory-queue-v1`  
**Confirmed CREATE commit:** `a6330989a43a872eeb7e689d4d06ebdf789d53a5`  
**Claim timestamp:** 2026-10-25T06:46:09.341Z.

## Mechanical design

Chassis-underside transverse forged axle with two open vertical kingpins, exposed bronze bushings, independently articulated left and right steering knuckles, six-stud wheel mounting flanges, structural steering arms, and twin telescoping hydraulic steering actuators. The hydraulic bodies pivot to follow their outer steering ball joints; piston rods extend or retract as the knuckles steer. This is a **visual articulated assembly**, not a gameplay steering controller.

**Scale:** Blender 1 unit = 1 metre (Unity 1 unit = 1 metre).  
**Mount/pivot:** `MOUNT_UNDERSIDE_Z0_UNITY_1M` at (0,0,0); chassis underside mount plane Z=0.  
**Axle:** lateral X; two kingpin axes at X=±0.68 m, Z=-0.36 m.  
**Steering:** both knuckles yaw around local Blender +Z, range ±24°. glTF transforms coordinate conventions on import (verify in Unity).  
**Motion:** 121 frames at 30 FPS; left/right knuckle rotation, left/right actuator aim, left/right piston translation/length. Motion is sampled from analytical endpoint geometry at each frame.

## Clearance and dimensional contract

- Fixed crossbeam ends at X=±0.605 m. The nearest moving hub section remains at least 0.18 m away in the longitudinal X envelope. Steering forks are attached outward of the fixed kingpin and beam.
- Kingpin radius 0.028 m; bearing inner envelope 0.041 m: **13 mm radial clearance**.
- Hydraulic tube inner radius 0.023 m; moving rod radius 0.016 m: **7 mm radial clearance**. A separate outer guide collar has a smaller 3 mm running clearance.
- Hollow barrel extends to local Z=0.300 m; rod starts at Z=0.245 m, retaining 55 mm nominal overlap at the barrel end. Actual moving rod length is computed from the changing joint separation.
- Analytically sampled steering poses: 121 frames × 2 sides = 242.
- 8 PBR materials: graphite steel, alloy, bronze, safety orange, rubber, ceramic, hardened steel, cyan indicator.
- Mount bolts, hubs and steering arm joints are deliberately assembled/contacting. The moving rod travels **inside the hollow barrel** rather than intersecting a solid cylinder.

The clearance tests check analytical envelopes and link lengths, **not** triangle-triangle self-collision, rigid-body contact or a Unity vehicle simulation. Steering motion and piston extension are baked for visual inspection; real gameplay should drive joint transforms via one authoritative controller.

## Files

- `generate_m02_022.py`: complete Blender `bpy` generator, mesh topology checks, re-entrant build, animated GLB and editable BLEND export.
- `qa_m02_022.py`: independent stdlib kinematic checks and actual binary GLB 2.0 animation inspection.
- `.github/workflows/ci-m02-022.yml`: isolated Ubuntu runner installing and executing real Blender.
- `generated/`: created only during CI; BLEND, animated GLB and QA JSON are uploaded as Actions artifacts (not claimed to exist before the workflow succeeds).

## QA and integration

```bash
python3 Tools/ModelFactory/M02/M02-022/qa_m02_022.py --math-only
blender -b -noaudio --python Tools/ModelFactory/M02/M02-022/generate_m02_022.py
python3 Tools/ModelFactory/M02/M02-022/qa_m02_022.py
```

Until real workflow evidence is checked, Blender/GLB/BLEND are **PENDING**, not PASS. Unity 6 import, EditMode, PlayMode, physics, visual review, FBX and full triangle collision remain **NOT RUN**. No existing scene, prefab, gameplay code, ProjectSettings, main, develop or other model packages were changed. PR must remain DRAFT until independent visual and Unity QA.
