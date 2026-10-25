# MODEL-04 / M04-016 — Corner microthruster array

Source of truth: \`Docs/ModelFactory/CATALOG_V1.md\` on \`feature/rrl-model-factory-queue-v1\`.
Claim commit: \`215aeafe2a4bbd0e6aeb5ecd8cdbfad2257eea04\`.

## Visual specification

Four **distinct ducted microthrusters** on a reinforced L-shaped corner bracket.
The two perpendicular mounting faces are **X=0 and Z=0**, with their common
mount pivot at \`(0,0,0)\`. +Y is up; +Z is forward; 1 Blender unit = 1 Unity metre.
The assembly extends into +X,+Z and down (-Y). No top-only mount.

Each nozzle has a real open-centre annular duct, ceramic lip, cyan emissive
rim, visible four-blade turbine, copper radial cooling fins and amber indicators.
The seven PBR materials include graphite, titanium, copper, ceramic, dark steel,
cyan emission and amber emission. The four \`NOZZLE_PIVOT_00..03\` empties
animate independent gimbal rotations (axes X/Z/Z/X, alternating phase),
between -12 and +12 degrees over 81 frames at 30 FPS.

The script is **idempotent within the Blender scene** (deletes generated scene
objects and materials before rebuilding). The geometry is visual only:
no thrust physics, gameplay components, Unity scenes or ProjectSettings.

## Reproduce

\`\`\`bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-016/generate_m04_016.py -- --out build/M04-016
python3 Tools/ModelFactory/M04/M04-016/qa_m04_016.py --out build/M04-016
\`\`\`

Expected: .blend, animated .glb, static .obj/.mtl, generator manifest,
independent GLB/OBJ QA JSON. Do not treat source presence as Blender execution.

## Collision QA limits

Conservative envelope for nozzle spacing: min centre spacing 0.39 m;
horizontal radial bound \`0.145+0.196*sin(12°)\` = ~0.1857 m per nozzle.
Expected conservative separation > 0.018 m. Pedestal/nozzle vertical
clearance > 0.11 m. These are analytic checks, not exhaustive swept-triangle
collision detection. Physical Unity import, colliders, runtime motion and
manual visual review remain separate release gates.

## Integration

Suggested path after review: \`Assets/RoboRougelikeModels/M04/M04-016/\`,
with Unity-generated .meta files, on a separate authorized integration PR.
Do not directly modify main scenes or other modules.
