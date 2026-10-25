# M03-020 — Kosz modułowy cargo

**Agent:** MODEL-03. **Catalog:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M03-020.json` on that same branch. This package is an isolated **visual-only** model; it does not implement inventory, mass, cargo physics or gameplay.

## Construction

An open-top rectangular cargo basket for the rear of a modular vehicle. Graphite corner posts and rectangular rims support an actual spaced titanium rod cage (not a flat texture), a nine-rod floor with five separate ceramic wear runners, copper outer stiffeners and two amber rear markers. Six authored PBR materials: graphite steel, titanium, ceramic, copper, rubber and amber. **No animation** is specified by the catalog.

The attachment plane is **Y=0**, and the basket extends toward **+Y**; **Z is up** in Blender. The root pivot is `(0,0,0)` on the rear attachment plane. Four `SNAP_REAR_*` locators are at `(±0.45,0,±0.20)` metres, each local +Z pointing toward Blender -Y (the vehicle). Unity units: **1 unit = 1 metre**; verify Blender-to-Unity axis conversion on import. Four rubber isolators, four copper pads and four metal fastener heads show the intended bolted rear attachment.

Nominal outer structure is about **1.12 m wide**, **0.70 m deep** between rim centres and **0.55 m high**. The rod cage is open on all sides and top; the floor uses real apertures. Vertical wall bars are 18 mm diameter at 110/115 mm spacing; horizontal reinforcement rods cross at intentional welded junctions. Floor rods are 22 mm diameter at 110 mm spacing. Ceramic runners have 148 mm free spacing between adjacent runner edges. Structural rod crossings, fastener seats and rail/post joints are **intentional contacts**. They should not be interpreted as errors by a naive AABB collision checker. The model has no moving parts.

## Source and validation

- `generate_m03_020.py`: repeatable isolated Blender `bpy` generator, real BLEND/GLB/FBX exporter, only deletes its own named collection and unused tagged data. No foreign objects are removed.
- `qa_blender_m03_020.py`: independently checks repeated builds, closed manifold positive-volume meshes, mounts, material counts, actual rod families and apertures, static status and foreign-object preservation.
- `qa_glb_m03_020.py`: independently parses the **exported binary GLB** and checks mesh/material/node counts, mount locators and absence of animation.
- `.github/workflows/ci-m03-020.yml`: isolated Blender 4.x export and QA, SHA256SUMS and retained CI artifacts.

To generate locally with Blender 4.x:

```bash
M03_020_OUT=/tmp/m03-020-output blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-020/generate_m03_020.py
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-020/qa_blender_m03_020.py
python3 Tools/ModelFactory/M03/M03-020/qa_glb_m03_020.py /tmp/m03-020-output/M03-020.glb
```

## Integration and outstanding gates

GLB and FBX are alternative import candidates, not approved Unity prefabs. Unity 6000.3.14f1 import, URP material appearance, rear socket orientation, cargo collision envelope, physics behaviour, PlayMode and EditMode require separate **real** tests. Do not infer those results from Blender mesh QA. No existing scenes, prefabs, ProjectSettings, game systems or main/develop branches are modified. Keep the PR **DRAFT** until integration review.
