# Known limitations / QA gate

- No Unity scene, prefab, ScriptableObject, gameplay, inventory, physics, socket authority, camera, economy or target ProjectSettings have been edited.
- GLB output contains ORIGINAL geometry with additional preview-only inert empty nodes. It is genuine binary modification but not a newly sculpted large series.
- Source Blender .blend is preserved as existing historical CI artifact; new Blender execution and FBX generation NOT_RUN in this runtime.
- M01 physical footprint mismatch; mass calibration unresolved. Neither source-only material extras nor sampler checks establish Unity URP shader/link PASS.
- Continuous moving triangle collisions/Physics.ComputePenetration and actual weak-side visibility under game camera NOT_RUN.
- Asset preview attachment cannot be treated as BuildSocket registration; MobilityHub pending authoritative adapter.
- ZIP and independent source target G0/G1/G2 must be verified; no merge, no live Codex game import.

- R1.1 corrects synchrony with one extra glTF animation clip; importer may ignore simultaneous clip semantics unless importer exports named clip and receiver actively selects it. Inspect 4 gimbals in Unity at 0/25/50/75/100% manually.
- New E2 121-sample vertex envelope is discrete time sampling, **not** continuous PhysX triangle collision; 3.622cm nominal per-side remainder on M04 requires actual rotation/scale/socket/mounted rig test.

- Source of original M04 artifact is historical GitHub Actions artifact 12586794828; GH artifact expiration/deletion can make rebuild-from-original fail later. Final R1.1 two-GLB ZIP is also published under a separate Actions artifact and must be archived by receiver with SHA256 if long-term support is needed. Never treat transient artifact ID as permanent binary source.

- **W13 confirmed source-data defect:** old M04 rest/animated "fits 3×3×2" claim was wrong under named W/H/D axis mapping; nominal depth 1.10m is exceeded by 0.47756m. Real M01 depth overrun 2.572m. W13 fail-closed source test blocks both models; geometric collision-safe candidate **NOT_YET_AVAILABLE**, no physical mount until design contract approval.
- Diagnostic 5cm per-face swept-size clearance is a QA proposal, **not** an authorized universal Build/PhysX threshold. Even alternative unapproved 3×2×3 for M04 falls short of this margin (3.622cm per lateral face). No permanent cell changes have been applied.
