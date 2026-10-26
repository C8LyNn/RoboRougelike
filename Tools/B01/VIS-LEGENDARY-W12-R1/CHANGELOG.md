# W12 R1 — 2026-10-25

- Actually edited M01-018 animated GLB in Git branch from original PR #195; preserved 53 meshes/7 materials/1 clip and attached inert WeaponBase/weak/aim helpers.
- Actually edited M04-024 original Actions-exported animated GLB locally; original 105 meshes/8 materials/4 gimbal channels preserved, added inert MobilityHub/weak/aim nodes; 13/13 real local tests PASS.
- Added source-safe GLB retarget script, negative suite, workflow to source original M04 CI artifact and build full two-GLB Codex ZIP, hashes, install/rollback and explicit native NOT_RUN gates.

## W12 R1.1 — 2026-10-26

- Corrected M04 animation semantics in the **actual GLB export**. Existing Blender output was four separate clips (one gimbal per clip); now retains four clips and adds `LEG-MOV-001_ALL_FOUR_GIMBALS` with four channels in one clip, preserving original binary mesh and sampler payloads.
- Added real GLB mesh-vertex pose sampler, valid quaternion interpolation/scene hierarchy and per-clip 121-sample swept envelope JSON included in Codex ZIP; positive+negative math regression suite.
- Updated SHA256/manifest/binding instructions, re-ran GitHub Actions; no original model regeneration, PhysX, Unity test or Codex game edits.
