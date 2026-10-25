# MODEL-03 / M03-019 — Osłona akumulatora przeciwodłamkowa

**Scope:** New modular visual asset for Unity 6 RoboRougelike. No battery gameplay, physics, existing prefab or scene modifications. Source of truth: `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`.

## Geometry and assembly

- **Mount:** inside vehicle frame, four underside `SNAP_BOTTOM_*` empties at X ±0.44 m, Y ±0.26 m, Z=0. Their positive local Z is the mounting normal.
- **Pivot:** `ROOT_MOUNT_Z0` at (0,0,0). Blender Z up, Unity conversion handled by importer; 1 Blender unit = 1 metre.
- **Envelope:** nominal 1.165 × 0.785 × 1.24 m including fully open leaves (approximate, verify in Unity). Structural body occupies ~1.17 × 0.79 × 0.78 m.
- **Visual design:** dark graphite anti-fragment steel, titanium structural members, pale ceramic appliqué, copper hinge hardware, rubber mounting feet, amber warning markings, green lock indicators. Seven PBR materials.
- **Hinged cover:** two independent leaves on side Y-axis hinges at X ±0.50 m, Z 0.755 m. Closed seam gap 18 mm. Each leaf rotates outward and upward by 105°. Latches rotate ±75° around local Z before opening and after closing. Animation frames 1–145, 24 FPS default.
- **Intentional physical interfaces:** fixed hinge barrels sit next to the door leaf edge; mounting plates touch rubber feet. The model is an exploded visual assembly with deliberate structural gaps, not a mechanically certified pressure vessel.
- **Collision policy:** closed manifold geometry for every mesh. 145 sampled frames verify both roof leaves clear the upper frame and each other; this is a conservative sampled envelope check, not continuous triangle–triangle collision detection. Latch-to-panel and hinge mechanical interfaces are not exhaustively checked.

## Build / QA

```bash
export M03_019_OUT="$PWD/m03-019-output"
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-019/generate_m03_019.py
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-019/qa_blender_m03_019.py
python3 Tools/ModelFactory/M03/M03-019/qa_glb_m03_019.py m03-019-output/M03-019.glb
```

Generator exports actual `M03-019.blend`, `M03-019.glb`, and `M03-019.fbx` to `M03_019_OUT`; CI uploads them as artifacts rather than committing generated binaries. Repeated-build QA verifies collection isolation, manifold topology, positive signed volumes, distinct materials, SNAP locations, door and latch animation, and sampled clearance.

## Unity handoff

Import GLB/FBX into an isolated test scene, check Z-up to Unity Y-up conversion, scale and material slots, keyframes, animation loops, visual fit to the chassis and battery, and physics collision meshes. Unity EditMode/PlayMode is **NOT RUN** unless separately evidenced. Do not merge the draft PR without independent QA and integration review.
