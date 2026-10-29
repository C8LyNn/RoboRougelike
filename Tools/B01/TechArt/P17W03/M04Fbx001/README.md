# B01-30 PASS17 / WAVE03 — M04-FBX-RECEIVER-001 R1

**Real candidate FBX asset**, not another lookdev image. Build a binary Autodesk FBX from the already existing original M04-024 Blender model and preserve the four **original** nozzle gimbal action tracks simultaneously in one FBX animation take. Do not add new geometry, gameplay, model authoring or MODEL_JOB.

## Pinned original producer
M04 original [Draft PR #196](https://github.com/C8LyNn/RoboRougelike/pull/196). Its exact source HEAD is eef02864b106546b200011a2f0286fe7d9a83a1f, generator Git blob 102b2d430260ae4da15fc7dee51f9bd86ccd822c and original QA Git blob 31ab3a7d79b5664bdfa9d8201ed26ed8a7104f62. The own Actions workflow fetches the immutable source, refuses changed IDs and executes the original Blender exporter and GLB independent binary QA. This source is never modified by B01-30.

## Reproduce actual binary asset
Run in isolated checkout with Blender and Python3. Check out original scripts from exact #196 commit under disposable build/B01-30/P17W03-source. The own .github/workflows/b01-30-p17w03-m04-fbx.yml specifies exact commands for original model creation, original QA, FBX export and independent importer. Main commands:

    blender -b --factory-startup --python-exit-code 1 --python build/B01-30/P17W03-source/Tools/ModelFactory/M04/M04-024/generate_m04_024.py -- --out build/B01-30/P17W03-original
    blender -b build/B01-30/P17W03-original/M04-024.blend --python-exit-code 1 --python Tools/B01/TechArt/P17W03/M04Fbx001/export_original_m04_fbx.py -- --out build/B01-30/P17W03-FBX --original-glb build/B01-30/P17W03-original/M04-024.glb
    xvfb-run -a blender -b --factory-startup --python-exit-code 1 --python Tools/B01/TechArt/P17W03/M04Fbx001/receive_original_m04_fbx.py -- --fbx build/B01-30/P17W03-FBX/M04-024-4GIMBALS-ORIGINAL.fbx --source-receipt build/B01-30/P17W03-FBX/m04-fbx-source-receipt.json --out build/B01-30/P17W03-FBX/reimport

## True receiving gate (Codex)
After exact source QA and B01-40/B01-60 independent receiving, Codex may import the real binary M04-024-4GIMBALS-ORIGINAL.fbx from the GitHub Actions artifact into an ISOLATED Unity 6000.3.14f1 URP project. Inspect root mount plane, source -Z exhaust vs converted Unity axes, scale 1m, 4 named pivot Transform objects, 30 FPS and animation frames 1/31/91. Confirm four nozzles animate in a single imported take. Unity ModelImporter/Animator may convert FBX axis/keyframes/materials differently: those acceptance checks are explicitly NOT_RUN by producer and must be native on licensed receiving host before any game install.

The package includes machine SHA256 receipts for actual FBX and original blend/GLB, original source mesh/vertex counts, independent Blender re-import four pivot angle deltas and 4 real Workbench PNG frames from the **imported FBX**. Its visuals are Blender Workbench only; materials are not certified URP shading.

No MODEL_JOB, no new M04 producer branch/version, no canonical Unity game source/Assets edits, no merge to main/develop. B01-40 provenance check, B01-60 independent receiver, B01-00 queues to #79, Codex ACK only if explicit.

**Rollback:** remove candidate FBX/materials/temporary prefabs from isolated receiver, restore earlier references, and delete disposable build/B01-30/P17W03-FBX. The original producer M04-024 GLB/blend remain untouched.
