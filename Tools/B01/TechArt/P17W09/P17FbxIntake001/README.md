# B01-30 — PASS17 WAVE09 — P17 ORIGINAL GLB → UNITY FBX INTAKE/R1

**Practical, actual Unity-importable visual proxy bridge to finally receive the immutable P17 visual models, not another QA-only harness, new MODEL_JOB, Collider, PARTSIZE source, or gameplay change.**

## Original sources / exact locks
- B01-23 original source three robots + bracing: [PR #375 at `5666a39eabd4cea91b2b605a9eb99ea2f1e226e8`](https://github.com/C8LyNn/RoboRougelike/pull/375), four original Git JSON blob IDs.
- B01-30 original real 3 GLB + 3 BLEND and 6 rendered PNG: [PR #423 at `60a453e4ecd915725bc2fac5433ba7ad43a628cb`](https://github.com/C8LyNn/RoboRougelike/pull/423), original [Actions run #41030346462 / artifact #12964979343](https://github.com/C8LyNn/RoboRougelike/actions/runs/41030346462/artifacts/12964979343). Their original 3 GLB bytes and per-file SHA are checked from original machine receipt, **NOT regenerated as substitute inputs**. Previous [binary W08 PR #460](https://github.com/C8LyNn/RoboRougelike/pull/460) is also unchanged; its independent-source-implementation tests confirmed 43 mesh nodes, 932 GLB triangles and all 5 amber bracing positions.
- The existing gameplay root `Assets/**` is **not touched** in this PR. No new game source or 3D asset identity is authored. Source and geometry are converted from previously generated original GLBs.

## Actual production outputs
1. **Three real FBX 7.x binaries** (Flanker/Bulwark/Skirmisher), directly derived by Blender import/export from the original 3 GLBs. Each original logical part remains individually named `P17PART__<source_localId>`. Original source transforms and original amber bracing material are checked in the imported GLB before export.
2. Independent **fresh Blender FBX reimport**, verifying all **43 original parts** (15+16+12), every original 0.55m source-cell translation, polygon counts and **5 amber Armor braces** (1+2+2), with 3 new actual FBX-derived rendered PNGs 960×720.
3. Actual deterministic **`B01-30-P17-ORIGINAL-VISUAL-FBX-UNITY-INTAKE.unitypackage`**, 4 embedded Unity assets (3 FBX and `RECEIVE_README.txt`), **4 stable `.meta` GUIDs**, exactly 12 original TAR members (each asset, asset.meta, pathname), exact SHA256 and per-source binary receipts. No source code in `Assets/` and no authored Unity scene/Collider.
4. Five actually executed fail-closed corrupt input tests: real FBX byte corruption, .meta GUID rewrite, foreign asset pathname, missing original third FBX and corrupted genuine gzip/tar archive. Two separate package builds from identical input must be identical. Ubuntu genuine build + Windows independent rebuild must be **byte-for-byte identical**.

**This package is Unity-importable FBX source, not native Unity G1 PASS.** Without actual licensed Unity 6000.3.14f1 Editor we have not observed the real Unity importer, FBX axis/material appearance, URP shaders, per-part identity import hierarchy, Collider or Rigidbody/PhysX. Native acceptance remains `NOT_RUN`, no B01-40/B01-60 formal acceptance, no Codex handoff or merge. The FBX geometry is still **placeholder visual diagnostic, not an approved physical size, collider envelope, socket or weakness simulation.**

## How to replay
GitHub Actions workflow `.github/workflows/b01-30-p17w09-p17-fbx-unity-intake.yml` obtains exact original source with `git fetch origin refs/pull/375/head`, original 3 GLB bytes from `gh run download 41030346462`, then executes:
```bash
blender --background --factory-startup --python-exit-code 1 \
  --python Tools/B01/TechArt/P17W09/P17FbxIntake001/export_source_locked_fbx.py -- \
  --original build/B01-30/W09/original --owner build/B01-30/W09/owner --out build/B01-30/W09/export

xvfb-run -a blender --background --factory-startup --python-exit-code 1 \
  --python Tools/B01/TechArt/P17W09/P17FbxIntake001/verify_real_fbx_reimport.py -- \
  --fbxdir build/B01-30/W09/export --owner build/B01-30/W09/owner \
  --out build/B01-30/W09/reimport

python3 Tools/B01/TechArt/P17W09/P17FbxIntake001/package_unity.py \
  --fbxdir build/B01-30/W09/export \
  --roundtrip build/B01-30/W09/reimport/p17-original-fbx-roundtrip-receipt.json \
  --out build/B01-30/W09/unitypackage
```

## Receiver and rollback
**B01-40:** receive the original actual GitHub artifact; verify original 3 FBX source SHA/bytes, 3 physical PNG images, 4 asset GUID/pathname UnityPackage members and 5 corrupt negative tests; compare exact Windows/Ubuntu package byte SHA and check no path/GUID conflicts against canonical Codex Assets tree. Only B01-40 may mark formal G0/offer to B01-00.

**B01-60:** use an **authorized isolated Unity 6 URP project**, import the genuine `.unitypackage`, inspect each of 3 drag-and-drop FBX models: 15/16/12 actual named meshes and original five amber material assignments, axes/scale/0.55m logical source positions, no duplicate GUIDs; explicitly mark G1 `PASS` or `RED`. Blender reimport is not Unity G1.

**Rollback:** delete only `Assets/B0130P17VisualBridge001` from disposable receiving project **after verifying no foreign prefab references**, remove only new build output `build/B01-30/W09/**`. Never alter B01-23 #375 or earlier B01-30 #423/#460 or Codex `Assets/**`. Source work this PR is isolated under `Tools/B01/TechArt/P17W09/P17FbxIntake001/**` plus own workflow.
