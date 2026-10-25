# M02-021 — Zwijana stopa podporowa

**Agent:** MODEL-02 (mechanical drive and wheels)  
**Claim:** `Docs/ModelFactory/claims/M02-021.json` on `feature/rrl-model-factory-queue-v1`  
**Claim CREATE commit:** `361aafaa3542faba2b11df4d71beaeda28aac90b`

## Mechanical design

A retractable chassis-mounted screw jack with a square telescopic column, four exposed cage rails, a rotating lead screw, travelling bronze nut, twin outboard guide rods, a 16-sided load-spreading foot and a rubber/ceramic ground contact surface. The open frame exposes the mechanism. Graphite steel, brushed alloy, copper, orange safety paint, rubber, ceramic, hardened steel and cyan indicator form eight PBR materials.

**Units:** Blender metres; 1 Blender unit = 1 Unity metre.  
**Pivot:** `MOUNT_UNDERSIDE_Z0_UNITY_1M` at `(0,0,0)`, mounting plane Z=0.  
**Axis:** Blender -Z points toward the ground; the screw and crown rotate around +Z. glTF converts to Y-up; verify orientation when integrating into Unity.  
**Animation:** 121 frames at 30 fps. At frame 1 the foot is retracted, frame 61 extended by 0.250 m, frame 121 retracted. Screw and drive crown rotate three turns forward and three turns back. Separate animation nodes: `SLIDE_TELESCOPIC_Z`, `LEAD_SCREW_ROTATE_Z`, `DRIVE_KNOB_ROTATE_Z`.

The travelling nut is deliberately shown with a radial running clearance; this is a visual mechanism, not a physically solved threaded contact. A gameplay controller should drive the foot translation and screw rotation consistently rather than simultaneously relying on imported baked animation and runtime physics.

## Dimensional and clearance envelope

- Outer cage minimum half-width opening: 0.086 m; slider half-width: 0.078 m, **8 mm side clearance**.
- Lead screw thread outer radius 0.028 m; nut bore radius 0.031 m, **3 mm radial clearance**.
- Nut outer radius 0.052 m; slider inner half-width 0.055 m, **3 mm radial clearance**.
- Guide rod radius 0.012 m; moving bushing bore radius 0.018 m, **6 mm radial clearance**.
- At full extension, column-to-cage axial overlap remains **0.110 m**.
- At full retraction, pad-to-cage axial clearance remains **0.038 m**.
- Guide-rod and screw engagement margins are checked across 121 poses.

Fastened components may touch or intersect intentionally; all *moving versus stationary* sliding envelopes above have positive clearance. Analytical envelope QA is **not** triangle-triangle collision detection, FEM, contact simulation, or Unity physics.

## Build and QA

```bash
python3 Tools/ModelFactory/M02/M02-021/qa_m02_021.py --math-only
blender -b -noaudio --python Tools/ModelFactory/M02/M02-021/generate_m02_021.py
python3 Tools/ModelFactory/M02/M02-021/qa_m02_021.py
```

The generator resets previous scene objects before each build and repeats construction in a single Blender session. It checks manifold geometry, degenerate faces and stable mesh/material counts; exports `generated/M02-021.blend`, `generated/M02-021_animated.glb`, and `generated/M02-021_qa.json`. Generated binaries are published as GitHub Actions artifacts, not committed.

**Integration:** No changes to existing scenes, prefabs, gameplay, ProjectSettings or other agents' packages. Unity 6 import, runtime articulation, collider authoring, collision stress test, physics and manual visual review are **NOT RUN** unless independently verified. FBX is not required because the animated GLB is the interchange deliverable.
