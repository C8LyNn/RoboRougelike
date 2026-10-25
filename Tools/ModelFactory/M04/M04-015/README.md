# M04-015 — Nóżka skokowa pneumatyczna

**MODEL-04 / Unity 6 RoboRougelike** — isolated **visual-only** asset. Source of truth: `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. Claim commit: `0661b5f85f28dbb51ee69d01954e9eb8012c4e22`.

## Silhouette and mounting

An oversized pneumatic jump leg with a **real hollow cylinder**, telescoping titanium piston, copper pressure reservoirs, graphite cage, eight radial heat fins, amber safety collar and a wide layered ceramic/rubber landing shoe. Seven independent materials, with cyan and amber emissive indicators used only as accents. The design has a thick visible piston rather than a flat decorative leg.

Unity scale **1 Blender unit = 1 m**. **+Y up, +Z forward, +X right**. The mounting surface is **Y=0**, with all geometry below the vehicle chassis. `MOUNT_UNDERSIDE_Y0` is at world origin. The `PNEUMATIC_STROKE_Y` animation pivot is also at origin; all piston/foot meshes are parented to it.

At 30 FPS the pneumatic assembly translates in Y: frame 1 = 0 m, frame 18 = +0.20 m (retract), frame 30 = −0.05 m (impact extension), frame 49 = 0 m (recover). Linear keyframes avoid overshoot. Stationary hollow housing has a **118 mm inner radius**, while the moving shaft has a **68 mm radius**, giving a **50 mm radial design clearance**. At full retraction the moving collar stays more than **120 mm** below the bottom stationary guard ring. The rubber foot stays clear of the fixed pressure canisters and guard. Internal contacts between intentionally assembled foot layers are not treated as errors.

## Reproduction

```bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-015/generate_m04_015.py -- --out build/M04-015
python3 Tools/ModelFactory/M04/M04-015/qa_m04_015.py --out build/M04-015
```

Output: `.blend`, animated `.glb`, `.fbx`, JSON manifest. The generator clears its Blender scene on each invocation, so repeated runs do not multiply objects. GLB has animated `PNEUMATIC_STROKE_Y`; FBX is available for Unity import. No scripts are imported into the gameplay project automatically.

## QA / limitations

The generator validates geometry presence, positive polygon areas, material count and analytical piston/guard clearances. Independent artifact QA validates real BLEND, GLB, FBX output, glTF animation translation channel, hierarchy, mesh/material counts and manifest. The GitHub workflow is responsible for executing both; **a committed workflow is not itself a PASS**.

**NOT RUN until proven:** Unity 6 EditMode/PlayMode, FBX/GLB import, shaded manual review, physics collisions and gameplay integration. Analytical clearance bounds are not a continuous triangle-level collision solver. Confirm animation, shading and physical interaction in Unity before production acceptance.

No scenes, ProjectSettings, existing modules or gameplay mechanics changed. Draft PR only; no merge.
