# B01-30 — PASS17 WAVE06 — P17-BRACE-VIS-001/R1

**A real 3D, measured-grid visual diagnostic for the existing B01-23 PARTSIZE/P17 kits, not a new MODEL_JOB or another model-receiver export.** Three physically generated GLBs and three native Blender .blend files depict **all 43 authored P17 part positions** (15 Flanker / 16 Bulwark / 12 Skirmisher); exactly **5 original Armor bracing parts** are painted amber, differentiated from standard Armor, Wheels/Tracks/Hover, Core and Gun. The independent receiver imports the actual GLB binary into a *fresh Blender process*, verifies all 43 original source world coordinates plus 5 brace marker props, and renders 2 actual 960×720 PNG viewpoints per rig. Originals remain owned by B01-23.

## Source of truth / provenance (immutable)

Read-only input [B01-23 Draft #375](https://github.com/C8LyNn/RoboRougelike/pull/375) @ `5666a39eabd4cea91b2b605a9eb99ea2f1e226e8` has 3 actual reinforced game-data JSON rigs and kit catalog. Our CI fetches precisely its current commit and **four exact original Git blob SHA1s**, never copies guessed data from main. `source_contract.py` verifies exact 43 logical parts and five source-declared braces, six **source-predicted** mitigations, original metadata, contact masks, zero rotations, unique cells, grid positions and expected types. Its twelve negative mutations must fail closed. No modified copies of the original B01-23 JSON go into this PR.

## Real generated assets

GitHub Actions produces (from original JSON without manual reauthoring):
- **Flanker**: 15 original part-space 3D nodes, one source bracing Armor highlighted.
- **Bulwark**: 16 original 3D nodes, two source braces highlighted.
- **Skirmisher**: 12 original 3D nodes, two source braces highlighted.

Each rig ships real `.glb`, `.blend`, ISO screenshot and TOP screenshot from **re-imported GLB**, with per-file SHA256 receipts and measured world-space source comparisons. The GLBs have material categories and named part object IDs so that a visual receiver can inspect source cell positions and brace identities. The nominal **0.55 metre logical cell grid** follows issue [#141](https://github.com/C8LyNn/RoboRougelike/issues/141); proxy boxes are inset at **0.42m** (not physics/collider geometry). Blender uses Z-up mapped from source Y-up; exported GLB is Y-up. Wheel/track/hover/gun are *distinctive visual placeholders*, not accepted physical dimensions or mechanical designs.

## Running source and actual 3D tests

```bash
git fetch --no-tags origin refs/pull/375/head
mkdir -p /tmp/B01-30-P17/source
for n in P17_Flanker_Reinforced.json P17_Bulwark_Reinforced.json P17_Skirmisher_Reinforced.json P17_StructuralBracingKits.json; do
  git show "FETCH_HEAD:Assets/PARTSIZE001/Bracing/P17/$n" > "/tmp/B01-30-P17/source/$n"
done
python3 Tools/B01/TechArt/P17W06/P17BracingVis001/test_source_contract.py --source /tmp/B01-30-P17/source
blender --background --python-exit-code 1 --python Tools/B01/TechArt/P17W06/P17BracingVis001/export_visual_rigs.py -- --source /tmp/B01-30-P17/source --out /tmp/B01-30-P17/export
blender --background --python-exit-code 1 --python Tools/B01/TechArt/P17W06/P17BracingVis001/receive_glb_and_render.py -- --source /tmp/B01-30-P17/source --export /tmp/B01-30-P17/export --out /tmp/B01-30-P17/receiver
```

The repository's separate `b01-30-p17w06-brace-visual.yml` workflow runs those actual commands in Ubuntu with Blender 4, verifies binary GLB/BLEND/PNG SHA receipts and uploads real outputs. An executed test at immutable PR SHA is required; script existence is not a PASS.

## Gates, scope, responsibility

**B01-23** (original PARTSIZE data owner): may inspect the visualizations to diagnose original logical grid cell placement, compare 5 brace positions/6 source-predicted loss mitigations, decide whether source design needs revision. No source modifications from B01-30. **B01-40** (independent source intake) validates actual 3 GLB, 3 BLEND, 6 PNG output byte hashes and 43 source identities. **B01-60** independently reimports GLBs or performs authorized Unity receiving; a producer-created Blender receiver is not independent B01-60 QA. **B01-00** controls queue, no Codex ACK required for independent visual QA.

**No canonical game edit:** changed Git files only under `Tools/B01/TechArt/P17W06/P17BracingVis001/**` and own CI workflow. No `Assets/**`, no rework of original #375/#391/#405, no gameplay code, economy, model catalog IDs, UV/LODs of source geometry or native colliders. **Blender source test PASS does not mean Unity Import PASS.** Materials/proxy shapes indicate source kind only, not actual PARTSIZE clearances, collider envelopes, wheels contacts, drive behavior, sockets or PhysX. Physical PlayMode and game integration **NOT_RUN**.

**Rollback:** remove this isolated diagnostic's generated files from a temporary receiving project; delete only `/tmp/B01-30-P17/` local build or dedicated `build/B01-30/P17W06/BraceVis001/` output. Never remove original B01-23 source files or any Codex assets. This is a visual fixture contribution to an approved PARTSIZE stream, not a new competing model release.
