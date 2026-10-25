# MODEL-QA-AUTO-001 / read-only automated 3D asset auditor — R1
HQ P1 dispatch / issue #160; B01-00 operates without any approved new Model Lab agent numbers. **No gameplay/Unity import or merge.**

## Actual independent evidence
- `catalog.json` contains 70 actual PR HEAD SHA from `M01–M05 × 001–014` and 14 exact historic fix PR HEADs, captured Oct 25 2026.
- `Docs/B01/MODELQA001/inventory-M0N.json` contains per-model actual Git tree sources/existing committed OBJ/GLB/FBX/BLEND and Git blob SHA1/size (baseline and fixes).
- `Docs/B01/MODELQA001/INITIAL_PROBE_EVIDENCE.json` contains independently EXECUTED GitHub source/primitive sweep prior to workflow: M05-002 original **755** AABB overlaps vs fixed **0**, 89 sampled poses x 1482 cross-group pairs per version. This is **broadphase sampled** evidence, not Blender animation collision certification.
- M02-001 generator changed from solid axle cylinders to ring-like hub, committed OBJ and GLB **unchanged SHA1** between original PR#40 and repair PR#50. It is a proven source/output *revision mismatch*, not proof of a corrupt rewritten GLB. Re-export required.
- M01-003 guide/shoe source changed in PR#102, but neither PR HEAD stores OBJ/GLB/FBX/BLEND; CI artifact claim needs immutable download+SHA validation.

## Isolated runner (standard Python 3.10+)
1. `git clone` normal read-only checkout; fetch pinned PR heads:
   `for id in 40 50 46 73 52 102; do git fetch origin "refs/pull/$id/head:refs/remotes/origin/modelqa/pr$id"; done`
2. `python Tools/ModelQA/v1/test_modelqa.py` — 12 selftest definitions, run result must be recorded honestly.
3. `python Tools/ModelQA/v1/modelqa.py --repo . --output modelqa_report`
4. `git show f7ab038346985a7d7d1437f7733bcf0257cf14bb:Tools/ModelFactory/M05/M05-002/geometry.json > modelqa_report/geometry.json`; `python Tools/ModelQA/v1/render_diagnostic.py modelqa_report/geometry.json --output modelqa_report/renders`
5. Outputs: `results.json`, `report.html`, six diagnostic orthographic SVG projections + five tilt poses. These projections are **not Blender rendered materials**. CI workflow `.github/workflows/b01-modelqa-auto001-r1.yml` implements the real read-only run and uploads a temporary QA evidence artifact; it is not permanent asset storage.

## Gating / honesty
Auditor's `BROADPHASE_CLEAR_SAMPLED` is only sampled primitive mesh AABB. Positive AABB can be false positive; future BVH / triangle narrowphase and adaptive interval sweep must be run in Blender for actual animated GLB, incl loaded scene sentinel and export. `STATIC_MESH_BASIC_PASS` is only finite vertices/faces/degenerate triangle check; it is not manifoldness, normals, UV, mount, physics, or watertight confirmation.
Unknown/missing transient CI binaries remain **EVIDENCE_MISSING**, not `READY`. No automatic approvals from JSON produced by source generators. No `ASSET_LIBRARY_READY` or `UNITY_VERIFIED` before artifact+Blender tests and independent signoff.

## R1.1 QA tool fix (not a model fix)
Historical M05-002 geometry.json stores animation keys `[-22,0,22,0]`, not two [min,max] limits. Initial auditor source wrongly destructured into two numbers. Fixed `sweep_json` to use min/max of all finite keys on commit `e38f581cfeefa25bba2ee5ed01c60ca6a9a76b41`; added the real-format regression case `test_actual_m05_four_angle_key_format_including_return` in `b54868cbe827478ae4307779647eb00ffe2f61f1`. No native Blender claim follows from this fix; GitHub Actions run confirmation required.
