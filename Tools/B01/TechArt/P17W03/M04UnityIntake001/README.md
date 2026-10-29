# B01-30 PASS17/WAVE03 — M04-UNITY-INTAKE-001/R2

**New tangible Codex candidate:** a real *binary Unity custom package* (.unitypackage) containing the original, Blender-tested four-gimbal M04-024 FBX (2,109,580 bytes), one opt-in Unity Editor import checker, and a receiving README. **Not** a new model or another lookdev screenshot. No gameplay code or new MODEL_JOB. Canonical Assets are untouched by this Draft PR.

## Exact original input and provenance

Previous producer [M04-FBX-RECEIVER-001 Draft #386](https://github.com/C8LyNn/RoboRougelike/pull/386) source commit 82ea17a6f964aa0cf08890c827b038ad9a0fc57f, verified binary artifact [GitHub Actions #40900769565](https://github.com/C8LyNn/RoboRougelike/actions/runs/40900769565/artifacts/12908284071); real FBX SHA256 a017534047b9db6a1001b32b186809b796d4e6562f1c9ea560042de97cf58cd3. Exactly 105 original mesh nodes and four animated gimbals were verified through a REAL Blender FBX reimport by producer (source G0), **not Unity**. This new pipeline downloads the real original artifact instead of regenerating or reauthoring the model; verifies original source receipts and content hash.

## True output and instructions

The CI builds **M04-024-SOURCE-LOCKED-UNITY-INTAKE-001.unitypackage**, standard deterministic gzipped TAR of three genuine Unity import assets, each with asset, asset.meta and pathname. Imported files are restricted to new **Assets/B0130M04Intake001/**. Unity 6000.3.14f1 can import the custom package through Assets > Import Package > Custom Package, **in an isolated QA clone only**. No automatic prefab or gameplay registration.

After importing, execute Tools > B01 > M04-024 > **Prepare and check candidate FBX**. This opts into Generic + animation-enabled + scale 1 importer options on this candidate path only; a separate Tools > B01 > M04-024 > **Check candidate FBX (read only)** validates:

- Exactly 105 MeshFilter components on the FBX model root.
- Four distinct named pivots N01_GIMBAL_PIVOT ... N04_GIMBAL_PIVOT.
- A SINGLE imported animation clip contains rotation curves for all four pivot paths (four separate clips are insufficient).
- For **each** pivot, sampled actual imported rotation curve must move away from the rest pose at 25% and 75% of clip duration, with **opposite signs** and >0.015 numeric delta; static, missing and one-sided data is rejected.

**WAVE04 R1→R2 actual defect:** R1 counted four *present* rotation bindings without examining their values. A real regression reproducer with four flat rotation curves was falsely GREEN; R2 checks numerical opposite-phase movement of all four gimbals in the same imported clip. Original source FBX and Blender QA are unchanged; R2 changes only this owned candidate Editor test and archive evidence. A C# shim test validates the algorithm, not Unity-native Animator performance.

A failing actual Unity import must be recorded as native G1 RED, with screenshots/logs. No shim success may be promoted as Unity import, Animator, URP/PBR visual, PhysX or G2 acceptance.

## Tests and receiving gates

The actual GitHub workflow source-locks #386 exact original HEAD and original FBX bytes. It independently decodes the real generated UnityPackage TAR/GZIP and verifies embedded FBX, GUID/meta/path names, deterministic byte-identical reproduction and negative corruptions. It compiles and executes the **actual original C# menu+checker** against explicit Unity Editor API shims, with fail-closed missing importer, model, mesh, pivot and animation conditions. These are real source-level tests **NOT native Unity G1**.

B01-40: verify original FBX binary SHA and producer source, path/GUID uniqueness, 3-file UnityPackage and rollback. B01-60: independently inspect output binary and run native Unity import + four-pivot common-clip test if an authorized runner exists. B01-00: only after appropriate acceptance, decide if and when to offer into CODEX INBOX #79. Codex: integrate only after explicit ACK; setup no gameplay logic.

## Rollback

In isolated recipient restore references first, then delete only Assets/B0130M04Intake001. In builder delete only build/B01-30/M04Unity001; do not replace original MODEL-04 art or M04 FBX PR #386 and do not merge to main/develop.
