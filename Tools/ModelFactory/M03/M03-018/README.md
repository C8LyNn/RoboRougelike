# MODEL-03 / M03-018 — Kardanowy uchwyt ruchomego modułu

Visual-only modular two-axis gimbal for Unity 6 RoboRougelike. This is **not** a new vehicle movement/gameplay system.

## Origin, scale, assembly
- **1 Blender unit = 1 metre**, Blender Z-up, Unity import orientation must be checked in-engine.
- Root pivot `M03_018_ROOT_MOUNT_Z0`: (0,0,0) on the underside of the upper chassis socket; mounting plate spans 0.76 m x 0.76 m.
- Four `SNAP_BASE_*` empties at x/y = +/-0.28 m and z=0, 0.56 m spacing. Their local +Z points into the assembly; check actual Unity socket orientation before using.
- `YAW_AXIS_Z` at world (0,0,1.00) m; yaw about local Z, **-45° to +45°**.
- `PITCH_AXIS_X` at (0,0,0.35) m relative to yaw, world z=1.35 m at rest; pitch about local X, **-25° to +25°**.
- `SNAP_PAYLOAD_TOP` at (0,0,0.10) m relative to pitch, rotates with pitch. This is a locator only, not a Unity prefab socket.
- Outer horizontal guard ring R=0.55 m, tube radius 0.047 m; inner vertical pitch ring R=0.28 m, tube radius 0.035 m. Conservative radial ring envelope clearance: **0.188 m**.
- The fixed bearing and yaw hub are distinct, with designed concentric bearing clearance; structural arms, saddle, spokes and accents intentionally meet their supporting parts.

## Construction
Procedural bevelled graphite frame, titanium arms, copper bearings and axle, ceramic saddle, rubber seal, cyan and amber emissive indicators. Seven authored materials. No external textures, no physics components, no changes to shared scenes or ProjectSettings.

## Build and QA
Run from the repository root on a host with Blender 4.x:

```bash
M03_018_OUT=/tmp/m03-018 blender -b --factory-startup --python Tools/ModelFactory/M03/M03-018/generate_m03_018.py --python-exit-code 1
blender -b --factory-startup --python Tools/ModelFactory/M03/M03-018/qa_blender_m03_018.py --python-exit-code 1
python3 Tools/ModelFactory/M03/M03-018/qa_glb_m03_018.py /tmp/m03-018/M03-018.glb
```

Outputs: `M03-018.blend`, `M03-018.glb`, `M03-018.fbx` under `M03_018_OUT`. The generator removes only its own collection and orphaned tagged data; it preserves unrelated scene objects. Animation runs over frames 1–121 at 30 FPS.

**QA boundaries:** Blender topology check verifies manifoldness and positive signed volume. Blender QA samples 121 frames for the inner-ring radial envelope, lower height, and rotation bounds. This is **not** a continuous mesh–mesh collision test. Structural contacts are intentional and not considered penetrations by the analytic ring-envelope test. Unity import, material fidelity, prefab socket fit, physics, and EditMode/PlayMode are **NOT RUN** until actual Unity evidence is available.

Claim: `Docs/ModelFactory/claims/M03-018.json` on `feature/rrl-model-factory-queue-v1`, created before source work. Source branch is isolated from `develop`; keep PR in DRAFT until independent review.
