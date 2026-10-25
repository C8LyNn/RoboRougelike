# M04-024 — Układ czterech dysz wektorowych

**MODEL-04 / Unity 6 RoboRougelike / visual asset only.** Catalog: `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. Atomic claim: `7ea2bd672c19b9db03532b291197164227a3960c`.

## Geometry and interface

Four individually gimballed, flared vector nozzles arranged in a symmetric 2×2 square under the vehicle. Each has a dark recessed throat, pale ceramic rim, titanium bell, copper cooling fins, cyan ion trace, amber status lamps and a rubber-isolated bearing. Eight distinct PBR materials. The root `ROOT_UNDERSIDE_MOUNT_Z0` is at **(0,0,0)** on the underside mounting plane **Z=0**. **+Z up**, exhaust toward **−Z**, **1 Blender unit = 1 Unity metre**. Nozzle centres are at X/Y = ±0.47 m, gimbal pivots at Z = −0.365 m. Mesh parts are rigidly parented to each nozzle's own pivot.

The four gimbals independently tilt around local **X** or **Y** axes by **−12° to +12°**, in opposing phase. Animation keyframes 1, 31, 61, 91, 121 at 30 FPS; no propulsion, steering or gameplay code is introduced. Model deliberately includes mechanical bearing contacts. The mounting plate is separate from moving nozzle assemblies.

## Reproducible generation and QA

```bash
python3 -m py_compile Tools/ModelFactory/M04/M04-024/*.py
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-024/generate_m04_024.py -- --out build/M04-024
blender -b build/M04-024/M04-024.blend --python-exit-code 1 --python-expr "import bpy; assert len([o for o in bpy.data.objects if o.type == 'MESH']) >= 75"
python3 Tools/ModelFactory/M04/M04-024/qa_m04_024.py --glb build/M04-024/M04-024.glb --manifest build/M04-024/manifest.json --report build/M04-024/qa.json
```

The generator clears the scene before each run, validates closed manifold geometry, counts assigned materials, samples 61 animation frames, checks pairwise moving-nozzle XY AABB separation and mounting-plate vertical AABB clearance, saves a native `.blend`, exports animated `.glb`, and emits `manifest.json`. An independent standard-library GLB parser checks the binary container, mesh/material counts, emissive materials and four independently animated gimbal rotation nodes.

**Scope of geometric QA:** sampled broad-phase bounding boxes; intentional bearing contacts excluded. This does **not** prove continuous triangle-triangle clearance, correct in-engine collision physics, URP shading or gameplay. Unity 6 EditMode, PlayMode, import and manual visual review are **NOT RUN** until actually executed. Blender export and tests are also **NOT RUN** until a successful CI execution is observed.

Do not edit main scenes, ProjectSettings or other agents' models. The source is confined to this isolated model directory and its own workflow.
