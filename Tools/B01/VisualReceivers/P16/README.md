# B01-30 — PASS16 original visual receivers

**PACKAGE_ID:** B01-30-TECHART-P16-RESEARCH-R1. **Owner:** B01-30. **PR:** #299, existing isolated QA branch. **Status:** RESEARCH_READY / NOT_OFFERED / NOT_INTEGRATED. [manifest.json](manifest.json) pins each original producer input and exact Git blob.

## Actual new outputs

- **Selected A1 wheel+arm research artwork:** Three new view-only vector+PNG poses from original B01-31 single-part SVG. Illustrator angles -12°, 0°, +12° are screen-space suggestions, NOT authorized suspension stroke. The original mount concept is held fixed and two original vector groups (arm+wheel) rotate around an unmeasured screen-space point. Damper is omitted because no real linkage is approved. A separate full 1920x470 contact image shows all three positions. [Actual source-raster CI #40720288734](https://github.com/C8LyNn/RoboRougelike/actions/runs/40720288734) SUCCESS; [rendered source/PNG archive](https://github.com/C8LyNn/RoboRougelike/actions/runs/40720288734/artifacts/12829353559).
- **Original M04 GLB four-action animated receiver:** The real producer Blender source is regenerated, independently GLB-validated, then reimported in a separate Blender 4.0.2 instance. Its four *original* independent animation actions are assigned to the corresponding imported gimbals (the default import activates only N01). Actual 4/4 angles at frame31: N01 X +10.899°, N02 Y -10.899°, N03 Y +10.899°, N04 X -10.899°. The real multi-action result is rendered into 6 orthographic PNGs plus a contact image. [Actual Blender CI #40720573781](https://github.com/C8LyNn/RoboRougelike/actions/runs/40720573781) SUCCESS; [rendered GLB screenshots](https://github.com/C8LyNn/RoboRougelike/actions/runs/40720573781/artifacts/12829374562).

## Reproduction and required original inputs

Execute only in a **disposable independent repository checkout**, not a live Codex gameplay worktree. Dependencies: Git, Python 3, CairoSVG/Pillow; for M04, actual Blender 4.x, NumPy, EGL/Mesa/Xvfb for headless render. Workflow [.github/workflows/b01-30-pass16-motion-art.yml](../../../../.github/workflows/b01-30-pass16-motion-art.yml) includes all exact executable steps, source-blob pins, fail-closed tests and artifact uploads.

A1 source: B01-31 #294 original commit 8709cf27901c4edca1434cbc760dec5db130a3ef; Git blob 96cc46a7c14f94a45c387e102d5940cb787d2d5c. Fetch owner PR #294 and run Python file Tools/B01/VisualReceivers/P16/selected_wheel_motion_proposal.py. Outputs: build/B01-30/P16-A1-motion-proposal/.

M04 producer: #196 original commit eef02864b106546b200011a2f0286fe7d9a83a1f; generator blob 102b2d430260ae4da15fc7dee51f9bd86ccd822c. Fetch PR #196, git archive its Tools/ModelFactory/M04/M04-024 directory to a disposable staging folder and run its unchanged Blender generator, then its unchanged qa_m04_024.py on the resulting 855236-byte GLB. Next call the own Tools/B01/VisualReceivers/P15/m04_actual_glb_contact.py with flags --glb /path/to/M04-024.glb, --out build/B01-30/P16-M04-four-clips, and --combine-clips; then invoke own Tools/B01/VisualReceivers/P15/compose_m04_contact_sheet.py on that output directory. Read the resulting M04-original-glb-lookdev.json. It must state four_animated_pivots_in_actual_blender_import = true and four unique combined_action_assignments. Real image artifacts must exist and be nonflat.

Rollback: delete only generated build/B01-30/P16-* outputs. Neither producer originals nor game Assets, CHL, PARTSIZE or Codex runtime are changed.

## Restrictions and consumer handoff

Research-only technical art, not new approved production models. Original A1 image coordinates do not prove real mounting face/socket/mass/wheel axle, chassis clearance, suspension or steering constraints. There is **no MODEL_JOB authorization** and no Unity prefab. Four genuine original M04 gimbal actions moving together in Blender DO NOT establish the correct runtime Unity animation controller or engine rendering/PhysX. The M04 producer alone owns its source model; B01-31 owns the original concept, HQ owns G-DESIGN/MODEL_JOB, and Codex alone owns canonical game integration.

Receiving agent: use the manifest and result images as a source-backed art reference only. Do not call the research receipt OFFERED, CODEX_CLAIMED, G1, G2, or a game integration; no main/develop merges. Native Unity/URP/PhysX, real 3D socket measurements and independent B01-60 acceptance remain NOT_RUN.
