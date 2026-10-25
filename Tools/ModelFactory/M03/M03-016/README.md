# M03-016 — Wspornik odwróconego trapezu

**MODEL-03 only** · Catalog: `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`.

## Construction

Rigid undercarriage support with **two parallel inverted-trapezoid side frames**. Each side frame has a 1.50 m top crossmember, a 0.96 m lower crossmember, and two **genuinely open, through-slotted diagonal beams** (four open beam slots total). Four fore/aft crossbars connect the two side frames. The lower edge sits at Z = -0.79 m. No animation: the catalog explicitly specifies *brak*.

The visual design uses five PBR materials: graphite structural steel, silver titanium slotted diagonals and mounting pads, pale ceramic lower wear shoes, copper bolt heads, and amber edge markers. The geometry is deliberately open: gaps and diagonal cutouts are actual mesh topology, not textures. Beams meet at shared surfaces without positive-volume penetration; joints are intended as rigid welded/contact assemblies, not physically simulated parts.

**Unity scale:** 1 unit = 1 metre. **Axes:** X width, Y longitudinal, Z up. **Root pivot:** (0, 0, 0), upper attachment plane. **Mounting direction:** +Z. **SNAP positions:** (±0.58, ±0.27, 0.044), four independent points (all sign combinations). No collider or gameplay changes are included.

## Sources

- `generate_m03_016.py` — deterministic stdlib geometry, triangulated OBJ/MTL/JSON, optional real Blender `bpy` GLB/FBX/BLEND export; safe repeated builds within a named Blender collection.
- `qa_m03_016.py` — independent manifold, winding, signed volume, 276 AABB pair checks, through-slot topology, SNAP and deterministic real OBJ/MTL verification.
- `qa_blender_m03_016.py` — actual Blender two-build idempotence and binary GLB inspection (meshes, materials, mounts), FBX/BLEND file checks.

## Commands

```sh
python3 Tools/ModelFactory/M03/M03-016/qa_m03_016.py
python3 Tools/ModelFactory/M03/M03-016/generate_m03_016.py --output /tmp/m03-016 --static-only
blender -b --factory-startup --python Tools/ModelFactory/M03/M03-016/generate_m03_016.py -- --output /tmp/m03-016
blender -b --factory-startup --python Tools/ModelFactory/M03/M03-016/qa_blender_m03_016.py -- --output /tmp/m03-016
```

The isolated CI workflow publishes the real exported files as workflow artifacts. Binary files are not committed to the source branch. Unity 6 import, visual fit, physics and EditMode/PlayMode require a separately activated Unity installation; absent that environment, report **NOT RUN**.

## Review gates

Inspect the four diagonal slots from both sides, four SNAP socket locations, material separation, and mounting fit on a real vehicle. Do not merge until Unity import and collision tests have been performed or the responsible integrator explicitly accepts their absence. No changes to existing gameplay, scenes, shared prefabs or ProjectSettings.
