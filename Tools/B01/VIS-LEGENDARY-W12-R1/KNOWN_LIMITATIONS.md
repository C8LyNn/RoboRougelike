# Known limitations / QA gate

- No Unity scene, prefab, ScriptableObject, gameplay, inventory, physics, socket authority, camera, economy or target ProjectSettings have been edited.
- GLB output contains ORIGINAL geometry with additional preview-only inert empty nodes. It is genuine binary modification but not a newly sculpted large series.
- Source Blender .blend is preserved as existing historical CI artifact; new Blender execution and FBX generation NOT_RUN in this runtime.
- M01 physical footprint mismatch; mass calibration unresolved. Neither source-only material extras nor sampler checks establish Unity URP shader/link PASS.
- Continuous moving triangle collisions/Physics.ComputePenetration and actual weak-side visibility under game camera NOT_RUN.
- Asset preview attachment cannot be treated as BuildSocket registration; MobilityHub pending authoritative adapter.
- ZIP and independent source target G0/G1/G2 must be verified; no merge, no live Codex game import.
