# M03-005 — Adapter ośmiokątny uniwersalny

**Catalog:** M03-005, "Adapter ośmiokątny uniwersalny"; frame sockets, rotating flange, eight mounting points. Source of truth: `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`.

**Claim:** `Docs/ModelFactory/claims/M03-005.json` on control branch. GitHub Contents CREATE succeeded at **2026-10-24T14:45:57Z**, claim commit **`9103d5f02c714f073a57b009e1ee50c3e2d2ca9e`**. Agent M03. This branch is exclusive to M03-005 and was created from `develop` commit `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.

## Design and mechanical interface

This is a **new** octagonal structural adapter, not another X frame. The fixed octagonal rear plate supports a solid central hub and a stationary precision bearing. A separate octagonal front flange rotates **360° around local +Y**. Eight hollow mounting bosses sit at 45° increments on a **350 mm bolt-circle radius**. Each has a **54 mm diameter physical through-bore** (not a painted dark disc), a ceramic wear lip, and a contrasting angular index sector. Four copper-coloured fasteners identify the rear mount.

**Coordinate system:** 1 Blender unit = 1 metre in Unity. +X right, +Y forward / insertion and rotation axis, +Z up. **Root pivot (0,0,0)** is a physical rear contact face of the solid mounting hub. Eight child Empty nodes `M03-005_PORT_OCT_00`…`07` have +Y normal, 27 mm bore radius, front contact Y=0.266 m, and are parented to the rotating flange. Model bounds X/Z ±0.413 m, Y 0–0.266 m.

**Five real material slots:** graphite steel, satin titanium, orange index paint, slate ceramic, copper fasteners. Blender adds 2 mm / 2-segment edge bevels and weighted normals; committed static OBJ is the unmodified source mesh. The frame/hub, rear bolts, rotating socket bosses and face lips are intentionally **separate watertight components**, joined by designed face contact or small welded overlaps. This is not a Boolean-unioned collider.

**Collision strategy:** the stationary bearing race reaches radius 116 mm, while the rotating flange's inner radius is 130 mm (**14 mm nominal radial separation**). Rear base plate and flange are separated axially; rotating socket bosses sit outside the static plate radius. Radial and axial interval checks prove stationary/moving separation at any angular position, with 73 sampled 5° poses as a reproducible diagnostic. Designed welded overlaps inside the *same* moving or stationary group are intentional, not animated clipping.

## Files

- `geometry.json` — explicit, inspectable 32-part geometry specification, eight moving socket metadata points and five material definitions.
- `generate_m03_005.py` — full procedural Python mesh generator, OBJ/MTL export, real Blender `bpy` animated rig, isolated collection-only `.blend` and selected-only animated GLB export; reruns clean owned objects, mesh datablocks and actions.
- `M03-005.obj`, `M03-005.mtl` — **actual committed 3D geometry** and material definitions, not an image or placeholder.
- `validate_m03_005.py` — independent parser of committed OBJ with manifold/face/material/source checks.
- `M03-005.static-qa.json` — committed static QA metrics, compared to fresh generator output in CI.
- `.github/workflows/ci-model-m03-005.yml` — deterministic mesh and actual Blender export QA with binary artifact uploads.

## Reproduction

```bash
python3 Tools/ModelFactory/M03/M03-005/generate_m03_005.py --self-test --output-dir /tmp/m03-005
python3 Tools/ModelFactory/M03/M03-005/validate_m03_005.py
# Blender-capable host:
blender --background --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-005/generate_m03_005.py -- --output-dir /tmp/m03-005-blender --require-blender
```

## Verified scope and limitations

**Local geometric prototype (2026-10-24):** source shape generation and actual OBJ export PASS. Independent Python `trimesh` verified **32/32 watertight meshes, 32/32 winding-consistent meshes and 32/32 positive signed volumes**. Actual mesh totals: **32 parts, 3498 vertices, 3600 polygon faces / 6944 triangles, five materials**, eight open sockets, eight rotating ports. Conservative stationary/moving clearance: **73 positions, 12,775 pair checks, zero crossings**. A GitHub Actions check validates the **committed** source and OBJ separately; use that result as CI evidence.

**Blender `bpy` / `.blend` / animated `.glb`: EXECUTED PASS.** [GitHub Actions run #40104556873](https://github.com/C8LyNn/RoboRougelike/actions/runs/40104556873) installed and executed real Blender, exported a binary `.blend` and animated `.glb`, verified 32 GLB mesh nodes, five material slots, eight moving port nodes, a rotation animation channel targeting the flange, and clean rerun (42 owned objects / 32 meshes / one action after two executions). Download real binaries from artifact **M03-005-Blender-GLB** (ID `12563649676`, 139,710 compressed bytes); static OBJ/MTL/QA from **M03-005-OBJ-MTL-QA** (ID `12564127435`). The CI's deterministic geometry and independent checked-in OBJ checks also passed. Local Blender executable was not installed.

**Unity 6: BLOCKED_UNITY_ACTIVATION / NOT RUN.** The PR's [EditMode](https://github.com/C8LyNn/RoboRougelike/actions/runs/40104556815) and [PlayMode](https://github.com/C8LyNn/RoboRougelike/actions/runs/40104556807) workflows failed at **Require valid Unity activation credentials**, before Unity started. No Unity import, physics colliders, gameplay binding, vehicle attachment or PlayMode test was performed. Static geometry and Blender GLB export do not constitute Unity runtime validation.

**Safety:** Draft PR only to `develop`. No direct changes to `main` or `develop`, no shared scenes, `ProjectSettings`, gameplay logic or other models. Review and Unity runtime checks are required before merge.
