# M05-016 — System lin holowniczych / Tow Cable Winch

**Catalog:** `M05-016` · **Agent:** MODEL-05 · **Scope:** visual asset only.

A rear-mounted industrial towing winch: graphite and titanium frame, copper ratchet, ribbed spool, seven-turn dark steel rope, four-roller fairlead, forged J-hook, ceramic guards and cyan status indicators. No gameplay systems, cable simulation, rope physics, or changes to existing sensors.

## Coordinate contract

- Blender / Unity scale: **1 unit = 1 meter**.
- Root pivot: `M05_016_ROOT_MOUNT_REAR_Y0` at **(0,0,0)**; mounting plane at **Y=0**.
- Tow/cable direction: **local -Y**; vertical **+Z** (Unity import axis conversion must be verified).
- Reel pivot: `M05_016_PIVOT_REEL_AXIS_X` at **(0,-0.43,0.32)**; rotation **local X**, keyframes **1,31,61** spanning 0–360 degrees.
- Four rear bolt markers at X=±0.31, Z=0.10/0.54; `MOUNT_MAIN`, `CABLE_EXIT_NEGATIVE_Y`, and `HOOK_REST` markers.
- Overall approximate static envelope: X=±0.56, Y=0 to -1.28, Z=-0.07 to 0.70 m. Validate actual evaluated bounds after export.

## Files

- `generate_m05_016.py` — deterministic bpy generator, local collection-only cleanup, closed mesh tubing, 8 materials, rotor animation, analytical clearance and manifold checks, GLB/BLEND/optional FBX export.
- `qa.json` — generated during real Blender run; not committed as an unverified PASS.
- `.github/workflows/ci-model-m05-016.yml` — CI Blender + GLB structure check and artifact upload.

## Generate

```bash
blender -b --python Tools/ModelFactory/M05/M05-016/generate_m05_016.py -- --out /tmp/M05-016
```

Outputs: `M05-016.blend`, `M05-016.glb`, `qa.json`, and `M05-016.fbx` if the Blender FBX exporter is available.

## QA contract and caveats

- Generator executes **twice in one Blender process** and checks that the mesh/material counts are stable.
- Manifold edges are checked for every mesh using bmesh. Three envelope clearances are checked analytically, then the reel is sampled for 61 frames. This is **not a complete mesh-to-mesh collision solver**.
- The free rope and hook are deliberately static while the reel rotates as a showcase animation. Cable payout/rewinding and tension would require a separate gameplay system and are **not implemented**.
- In Blender, bevel modifiers and weighted normals are present on hard-surface components; confirm final shading after Unity GLB/FBX import.
- Do not report Unity EditMode, PlayMode, or URP visual tests as PASS unless executed in Unity 6.

**Claim:** `Docs/ModelFactory/claims/M05-016.json` on `feature/rrl-model-factory-queue-v1`.
**Claim commit:** `73dcd84d1b470f6101394d00c9f73d58263759a4`.
