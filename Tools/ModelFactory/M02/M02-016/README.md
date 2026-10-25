# MODEL-02 / M02-016 — Przekładnia stożkowa kątowa

Standalone visual-only Blender asset for Unity 6 RoboRougelike. Source of truth: M02 queue in Docs/ModelFactory/CATALOG_V1.md on feature/rrl-model-factory-queue-v1. Claim committed before model production.

## Layout

Two illustrative straight-bevel gears with 16 and 24 teeth, orthogonal +X input / +Y output shafts and common pitch-cone apex (0,0,0.46) metres. Nominal speed ratio -16/24 = -2/3; pitch cone half-angles 33.690 and 56.310 degrees. **1 Blender unit = 1 metre**, +Z up. Fixed chassis mounting datum and pivot (0,0,0), four bolt locations, two separate bearing pedestals and named shaft sockets. 7 PBR materials: graphite, titanium, hardened steel, copper, rubber, amber and cyan service lamp.

Input rotates 720 degrees and output -480 degrees over 121 frames at 30 FPS. Both gears, their bolts and shafts inherit animation from dedicated parent empties. The tooth profile is an intentionally stylized straight-bevel preview, **not** a manufactured involute gear; it must not be used to infer rated torque, contact stress or efficiency.

## Build and test

Run the pure-Python test first:

    python3 Tools/ModelFactory/M02/M02-016/qa_m02_016.py

Then generate and validate using an actual Blender installation:

    blender --background --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M02/M02-016/generate_m02_016.py -- --out /tmp/M02-016
    python3 Tools/ModelFactory/M02/M02-016/qa_m02_016.py --verify --out /tmp/M02-016

Generator produces .blend, animated .glb, QA .json and attempts animated .fbx. CI uploads actual binary exports as artifacts; only source, tests, documentation and workflow belong to this branch. Geometry QA checks closed manifolds; Blender BVH checks **cross-gear triangle intersections at 13 discrete frames**. This is not a continuous swept collision test or a PhysX dynamics simulation.

## Unity integration

No gameplay changes, existing assets, common scenes or ProjectSettings are modified. Unity 6 import, URP visual inspection, animation-axis conversion, colliders and vehicle physics must be validated by the integration owner. Exporting GLB/FBX is not evidence of a successful Unity import.
