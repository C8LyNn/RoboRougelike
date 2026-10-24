# M03-002 — Telescopic structural beam (1×2×1 m)

Source: MODEL-03 exclusive queue. Rear frame mount at Blender local (0,0,0); beam longitudinal axis +Y, Blender +Z up. Retracted envelope X=1m, Y=2m, Z=1m. Front frame mount (0,2,0) when retracted; extension +0.55m along local +Y, with minimum 0.29m sliding overlap. Six defined materials; actual geometry uses four: graphite, titanium, ceramic and safety-yellow. Yellow calibration stripes are attached to the sliding component. This is a visual asset, not a new vehicle physics mechanic.

## Files

- `generate_m03_002.py`: reproducible Python generator, stdlib static OBJ/MTL and bpy animated .blend/GLB.
- `M03-002.obj` and `M03-002.mtl`: static retracted geometry produced from the same declarative component specification; editable Blender generator is the animation source.
- `M03-002.static-qa.json`: conservative 21-position sliding-vs-fixed AABB checks; no Blender/Unity runtime verification.

## Generate / validate

```bash
python generate_m03_002.py --self-test --output-dir /tmp/m03-002
blender --background --python generate_m03_002.py -- --output-dir /tmp/m03-002 --require-blender
```

In Blender, use `M03-002_MOUNT_REAR` as rear port and `M03-002_MOUNT_FRONT` as moving front port. Slider `M03-002_SLIDER` animates Y=0 to Y=0.55m (frames 1–30), then returns by frame 60. The two tubes are deliberately nested with 0.034m minimum lateral clearance from the external guide strips to the sleeve interior; the rear connector does not touch the sliding assembly. Bevel modifier is applied to render geometry. Static OBJ is box-faced and not beveled; for game-ready visual evaluation generate Blender GLB.

## QA status

- Static component dimensions, overall retracted 1×2×1 envelope, 21 sampled extension positions, conservative AABB moving-vs-fixed collision checks: PASS (see JSON).
- Python generator syntax/runtime: NOT RUN in CI at PR creation; run locally or in CI for confirmation.
- Blender geometry/animation/GLB export, Unity import, EditMode/PlayMode and visual QA: NOT RUN.
- Unity GLB importer axis conversion should be checked: Blender Z-up, +Y longitudinal. No gameplay code, scenes, or ProjectSettings were modified.
