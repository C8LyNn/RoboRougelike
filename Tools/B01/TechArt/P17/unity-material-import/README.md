# B01-30 / PASS17 — MAT-IMPORT-001 R1

**Actual new functionality:** offline builder of a self-contained native Unity custom package from **eight original MAT-A1-001 PNGs**, with an opt-in Unity Editor menu which creates/reuses **two URP Lit Material assets**. Does **not** create a new 3D model or modify any gameplay-owned file.

## Input provenance

Original MAT-A1-001 R1 producer: [Draft PR #336](https://github.com/C8LyNn/RoboRougelike/pull/336) commit `171983f85ff41df6ef33b95939cc877ba68e03ca`, original material generator Git blob `171d53bc7d5a4840a067af5c934a8fd1c7ad774e`. Workflow fails closed if original owner SHA or generator changes. Original generator emits 8 PNGs and SHA256 manifest, then this new tool ingests those exact pixels and does not manipulate/tint them.

## What the generated binary contains

The actual file `B01-30-MAT-IMPORT-001-R1.unitypackage` contains Unity-compatible `GUID/asset`, `GUID/asset.meta` and `GUID/pathname` tar/gzip members for **10 assets** (8 original PNG texture files, one C# Unity Editor importer, one TXT instruction). All GUIDs are deterministic and unique, and all imported paths are under the **new dedicated folder** `Assets/B0130MATPack01/**`. No canonical game Assets files, shaders, scenes, prefabs, or 3D meshes are modified by the producer.

After authorized import, the opt-in menu `Tools > B01 > MAT-A1-001 > Create candidate URP materials` verifies all eight imported textures and URP Lit shader before making changes, configures BaseMap sRGB, linear normal/metallic/occlusion, mipmaps and repeat, then creates or updates the two .mat files `Assets/B0130MATPack01/Materials/tire_rubber.mat` and `brushed_arm_alloy.mat`. Second invocation is idempotent: updates the same references, does not duplicate assets.

**Target:** Unity 6000.3.14f1 / Universal Render Pipeline. This is a candidate lookdev receiver, not an approved final robot material or a core game change.

## Build and verify WITHOUT Unity

Run only in an isolated checkout; no Codex active worktree modifications. Python 3.10+ and original MAT-A1 generator; Pillow used for original PNG contact sheet. The GitHub Actions workflow pins and runs the original generator automatically.

1. From original MAT-A1-001 source @ frozen SHA, run `python3 Tools/B01/TechArt/P17/materials/generate_materials.py --size 512 --out build/B01-30/MAT-IMPORT-001-original`.
2. Run `python3 Tools/B01/TechArt/P17/unity-material-import/build_unitypackage.py --original build/B01-30/MAT-IMPORT-001-original --out build/B01-30/MAT-IMPORT-001`.
3. Run `python3 Tools/B01/TechArt/P17/unity-material-import/verify_unitypackage.py --original build/B01-30/MAT-IMPORT-001-original --built build/B01-30/MAT-IMPORT-001`.

Checks read the **actual tar/gzip package**, assert all original PNG bytes and SHA identities, stable/unique Unity GUIDs, importer metadata and proper slot names. Three adversarial negative controls must reject changed PNG payloads, changed meta GUID and omitted original PNG. A second build must be binary-identical.

The CI also compiles the **actual original C# importer** against explicitly declared Unity API shims with Mono MCS and runs checks for idempotent material creation, per-asset channel assignment and fail-closed behavior on missing source PNG or absent URP shader. This is **not a licensed Unity Editor compile/test**, but is stronger than static source inspection.

## Receipt, handoff, rollback and gates

Expected generated artifacts: real `.unitypackage` binary, per-asset SHA256/GUID receipt, independent verified receipt, job logs. Generated data lives under `build/`, not tracked in git. [Package manifest](manifest.json) identifies author and source SHA, exact scope, file inventory and dependencies.

**Receiving flow:** B01-40 source-scope/manifest validation → B01-60 independent QA → B01-00 ordering → Codex only if approved. On authorized isolated Unity host, use Assets > Import Package > Custom Package, pick the real artifact, then manually run the importer menu above. Inspect created Materials and actual URP render/UV seams, verify no project file collisions. Do **not** regard a successful tar/Mono run as Unity G1 or integration.

**Rollback:** In an isolated recipient, restore prior references (if any), then delete only `Assets/B0130MATPack01`; in builder delete `build/B01-30/MAT-IMPORT-001*`. No merge to main/develop, no claiming OFFERED or CODEX_CLAIMED from a Draft PR alone.

**Limitations:** no new MODEL_JOB, shader visual output and in-engine import NOT_RUN, no model-specific UV/physically based look acceptance, Blender/PhysX not applicable, independent B01-60 acceptance pending, final art selection still HQ provisional. The Unity shader must be installed and available; script aborts if not.
