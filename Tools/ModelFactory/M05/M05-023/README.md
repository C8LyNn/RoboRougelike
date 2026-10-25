# M05-023 — Kontener części zamiennych / Spare Parts Container

**Queue:** M05; **mount:** rear chassis, `M05_023_MOUNT_ROOT` at `(0,0,0)`.
**Unit convention:** 1 Blender unit = 1 metre; +Z is up; +Y points away from the chassis.

## Appearance and construction

Industrial service container with graphite load-bearing frame, ceramic doors,
titanium rails, copper hinges, rubber anti-slip feet, orange safety markings,
green/red status LEDs and six individually detailed spare-part trays. The back
adapter exposes four mounting markers (L/R × LOW/HIGH). An additional
`CARGO_ACCESS_FORWARD` marker identifies the door service zone.

Both doors swing **outward** around their own vertical Z pivots at
`(-0.75, 1.015, 0.60)` and `(+0.75, 1.015, 0.60)`. Animation:
frames 1–30 open to ±105°, frames 30–60 hold, frames 60–90 close.
At 30 FPS, this is a three-second demonstration cycle.

This is **visual geometry only**. No inventory, repair, cargo transfer,
collision physics, or gameplay logic is implemented.

## Reproduce

```bash
blender --background --factory-startup --python Tools/ModelFactory/M05/M05-023/generate.py
python3 Tools/ModelFactory/M05/M05-023/validate_glb.py Tools/ModelFactory/M05/M05-023/out
```

The generator writes `.blend`, `.glb`, optional `.fbx`, and `qa_report.json`
to `out/` (or `M05_OUTPUT_DIR`). It does not commit binary exports.
The CI workflow publishes these files as downloadable build artifacts.

## QA scope

- Manifold checks for every mesh using Blender `bmesh`.
- Repeat generation in one Blender session, checking stable object/material counts
  and preservation of a foreign sentinel.
- 121 analytic samples of leaf-to-leaf and leaf-to-front-rail clearance.
- Independent GLB container, animation, materials, node and pivot-marker check.
- **Limitations:** analytic door checks are not exhaustive all-pairs mesh collision
  detection; intentional hinge-to-door attachment overlap is excluded.
- **Unity:** import, URP visual inspection, EditMode and PlayMode tests remain
  `NOT RUN` until an actual Unity runner and activation are available.
- **FBX:** optional; the GLB and BLEND files are the required exports.

## Isolation

Files are confined to `Tools/ModelFactory/M05/M05-023/` and this model's
GitHub Actions workflow. No scenes, ProjectSettings, existing mechanics,
other model packages, `main`, or `develop` are modified.
