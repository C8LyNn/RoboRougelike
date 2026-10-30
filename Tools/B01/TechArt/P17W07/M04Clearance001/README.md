# B01-30 / PASS17 WAVE07 — M04-TRIANGLE-CLEARANCE-001/R1

**Genuinely executed narrow-phase inter-nozzle collision test for existing immutable M04-024 original FBX**, not another similar source asset, another MODEL_JOB, a new gimbal mesh or a game/prefab edit. The prior producer source [#196](https://github.com/C8LyNn/RoboRougelike/pull/196) establishes original 4 moving nozzle pivots. Its earlier exact original FBX [#386 @ 82ea17a6f964aa0cf08890c827b038ad9a0fc57f](https://github.com/C8LyNn/RoboRougelike/pull/386) and the Unity import helper [#399](https://github.com/C8LyNn/RoboRougelike/pull/399) verify 4 gimbal angles but did **not** prove geometric mesh-to-mesh clearance through all interpolated sample frames.

## What this produces

Actual GitHub Actions job downloads the **original already binary-produced FBX** from [#386 Actions #40900769565](https://github.com/C8LyNn/RoboRougelike/actions/runs/40900769565), 2,109,580 bytes, SHA256 `a017534047b9db6a1001b32b186809b796d4e6562f1c9ea560042de97cf58cd3`, and original source receipt. It fails closed for drift from frozen #386 HEAD / original exporter Git blob.

A **fresh Blender** process loads the exact binary FBX and verifies all 105 original mesh nodes, four real imported animated pivot actions and their two-sided 7°–15° sampled tilt at frames 31/91. It computes transformed world-space source triangle BVH hierarchies for every moving nozzle at **all 121 integer frames**; performs **6 distinct inter-nozzle pair checks per frame (726 opportunities)**, with real narrow-phase triangle overlap whenever bounding boxes can touch. It reports each frame/pair in real machine-produced CSV with conservative AABB lower-gap bounds and actual hit count. It intentionally inserts **one original nozzle into another in an isolated in-memory mutation**, requiring actual triangle overlaps, and separately mutates the FBX binary to force source hash rejection. It restores original pose before the positive sweep. It produces a machine SHA256 receipt and **three actual Blender Workbench screenshots** from the imported FBX at frames 1/31/91.

## Execute exactly

```bash
# Requires gh CLI authenticated to read the existing GitHub Actions artifact:
mkdir -p /tmp/m04-original
gh run download 40900769565 --repo C8LyNn/RoboRougelike --name B01-30-P17W03-M04-024-REAL-FOUR-GIMBAL-FBX --dir /tmp/m04-original
xvfb-run -a blender -b --factory-startup --python-exit-code 1 \
  --python Tools/B01/TechArt/P17W07/M04Clearance001/sweep_original_fbx.py -- \
  --fbx /tmp/m04-original/M04-024-4GIMBALS-ORIGINAL.fbx \
  --receipt /tmp/m04-original/m04-fbx-source-receipt.json --out /tmp/m04-bvh
```

The repository's dedicated workflow `.github/workflows/b01-30-p17w07-m04-triangle-clearance.yml` pins original producer SHA, downloads prior original artifact, runs Blender with EGL/Xvfb, checks output independent CSV/PNG/SHA and uploads machine receipts. **Only a green run on exact HEAD makes the result PASS.**

## Interpretation / rigorous limits

* Unlike original AABB-only source validation (61 frames), this evaluates **transformed actual rendered mesh triangles** in 121 discrete FBX-imported frames. It detects actual triangle-triangle intersections between **different moving nozzle assemblies**.
* It is **not a mathematically continuous CCD sweep**: sub-frame motion between these frames is untested. The AABB gap values are lower bounds, not precise minimum triangle-to-triangle distances.
* Mount plate, fixed socket, spindle and intended mechanical bearing contact **not** certified for nonintersection, nor same-nozzle intentional nested geometry. This is not mechanical fit/collider/typed socket/PhysX proof.
* Actual Unity 6000.3.14f1 ModelImporter, Animator, URP, materials, scale/axis after import, Unity native EditMode/PlayMode, actual physical detach, gameplay and Codex integration remain **NOT_RUN**. B01-40/60 independent receiver QA has not been replaced by producer tests.
* Source and gameplay never change. Paths are limited to this own `Tools/B01/TechArt/P17W07/M04Clearance001/**` and one own workflow. No new `Assets/**` or producer source rewrite.

## Handoff / rollback

**B01-40** independently inspect original #386 FBX hash, full 726-row CSV and negative hit receipt; verify source Git diff scope. **B01-60** may independently rerun actual Blender BVH with separate environment, inspect three screenshot frames and require repeated whole-frame/negative evidence. **B01-00** decides Codex inbox priority only when receivers formally approve; Codex is currently busy. Deliverable is source-locked art validation, not a new importable gameplay prefab.

Rollback only the generated `build/B01-30/P17W07/output/**` QA results, no original binary changes. If temporarily loaded into Unity, remove only the isolated candidate test asset and references, never Codex or original MODEL-04 source.
