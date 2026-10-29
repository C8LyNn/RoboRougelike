# ENV04-001 / v002 — one HQ-authorized refinement, before/after and real QA

**Date:** 2026-10-29. **Status:** `REFINEMENT_V002_DONE / BLENDER_QA_PASS / ART_REVIEW_PENDING / UNITY_NOT_RUN / NO_MERGE`.
One existing static industrial ventilation crate; no new pilot, no new claim, no gameplay source or Unity integration. Original CC0-1.0 provenance retained.

## Governance and ownership

Reviewed #276, #407, existing open Draft #289 and its reviews/comments before editing. No #289 review comments, no ENV04 OFFERED_FROZEN/CODEX_CLAIMED evidence in checked #79/#287/#407; HQ explicitly authorized this single v002 rework. No CODEX_ACK asserted. No modification of v001 sources, v001 binaries, claim or canonical Unity. Exact v001 artifact is retained in `generated/`, and new v002 in `generated/v002/`.

- **Original v001 source commit:** `dca1cfe4407f36b05fa0717b959e7c7ee41dbcc4`; original published PR HEAD `09c3c925c4a01359cf15a487ca90bc0efbe1fd23`.
- **v002 generator/QA source commit:** `caea4019d02727a469a014250f9ba55ba45a154c` (physical `generated/v002/SOURCE_SHA.txt`).
- **v002 physical binary publication commit:** `dac170baef1b852af248081e93e97a903aa19522`.
- **v002 source generator SHA-256:** `fa41f1c88e3d9e256af1061c1aeb868895b6defa63b47e2665e0dda3a93f633f`.
- **v002 QA script SHA-256:** `a60acf06574a8adb28b0b9373b5b0735df0f661bcbd9c4bd914dc59940193db7`.

## Actual geometry fixes

- Preserved **six fixed 28-degree louvers**, narrowed each from 105 mm to 85 mm in local Y for more visible slots, and added **12 flush 21-mm end brackets** that connect slat ends to the side rails.
- End rails shortened from 1.420 m to 1.342 m, meeting the side rails at a butt joint instead of interpenetrating at corners.
- Posts now rest at z=0.130 m on the plinth instead of penetrating it. Recessed floor now rests flush on the plinth instead of hovering 7.5 mm above it; floor inset avoids posts.
- Side/end shell panels inset from posts and one another; amber markers seated against the end armor, rather than slightly intersecting it.
- Added explicit `UV_METRIC_1M` per mesh, world-projected by face normal at **one UV tile per metre**; no external textures. All dimensions and normals checked after applied rotations/scales.
- Rendered original-angle **front and rear** comparisons plus **top and improved raised side** views, all actual 960×640 PNGs. Shallow side preview was visually rejected and rerendered at a higher angle.

| Metric | v001 before | v002 after |
|---|---:|---:|
| Mesh objects | 28 | 40 |
| Triangles | 1200 | 1344 |
| Materials | 3 | 3 |
| Blender AABB | 1.56 × 1.02 × 0.531 m | 1.56 × 1.02 × 0.531 m |
| Root pivot | (0,0,0) ground | (0,0,0) ground |
| UV | primitive defaults | metric projected UV, validated |
| Animations | none | none |

Blender +Z up / +Y front / +X width, 1 Blender unit = 1 metre; GLB exported +Y up, Blender front maps to GLB -Z. Proposed **UNVERIFIED** BoxCollider in Blender frame: center (0,0,0.27), size (1.65,1.08,0.58) metres.

## Physical before/after evidence

**v001 immutable originals** (in `generated/`): `preview_front.png`, `preview_rear.png`, `ENV04-001_industrial_vent_grate.blend` SHA-256 `0f87a1b7386a0c8cb836ffd393b187dd177d523a39a8c3d87cd64e48d4893632`, GLB SHA-256 `b69e6466a115bf328e99961df452205c9a51f07f34e1e9ebc2de6a7ddf5f349d`.

**v002 physical outputs** (in `generated/v002/`): `ENV04-001_industrial_vent_grate.blend`, `ENV04-001_industrial_vent_grate.glb`, `preview_front.png`, `preview_rear.png`, `preview_top.png`, `preview_side.png`, `build_report.json`, `qa_report.json`, `SOURCE_SHA.txt`, `SHA256SUMS`.

**v002 SHA-256:**
- BLEND `68b042c7f2a22b95a04f54cc78dffa0445ada4cc9d4a9ccfc2acff749b3352ea`
- GLB `5e367958b7537c7a37fcdb74311cdc58f65716117f7f498475d25ca407cc757d`
- Front `e3de11b3f40ade28a524a3c1cb87a595b6222d92b3dfeaf135b850e18ad67dc3`
- Rear `229a9cf96ee12302819149d92da67385d85d437b31a8b0cba8b00ab12bd5def7`
- Top `1b89395d3a20169d8a280ef224a77ec1d1bbd43bade31727cdfe11db180807f2`
- Side `674f1d8c0f2804afc56ad37ef7544a4a5a35e03ea802e9295acbcb00f7f851e1`

**Exact physical CI:** [push Actions #40969369152](https://github.com/C8LyNn/RoboRougelike/actions/runs/40969369152) SUCCESS; [PR Actions #40969373575](https://github.com/C8LyNn/RoboRougelike/actions/runs/40969373575) SUCCESS; [physical artifact #12940490269](https://github.com/C8LyNn/RoboRougelike/actions/runs/40969369152/artifacts/12940490269). The first v002 reproducibility attempt was RED due only to QA's `Path.relative_to(BASE)` for /tmp; corrected in v002 QA, then both independent passes GREEN. Final Blender 4.0.2 generator executed twice in separate directories and both QA passes completed.

## Executed test matrix

- **PASS** actual Blender 4.0.2 BLEND save/reopen, actual GLB 2.0 binary parse, 4 PNG renders, 40 meshes, 1344 tris, 3 materials, root pivot, metric scale.
- **PASS** zero nonmanifold edges, zero degenerate faces, zero nonpositive-volume meshes, zero invalid normals, zero invalid UVs; **55 critical AABB pairs** checked with zero positive-volume intersections.
- **PASS** two independent Blender generations matched geometry/UV SHA-256 `b8eb8901786886f6473370febd343ead08e3a1db5df1a192e3173c851012b3d7`, triangles, materials, AABB and source hash. GLB byte hash identical between the two final runs. Workbench PNG and BLEND byte hashes are not promised bitwise deterministic; semantic geometry is.
- **PASS** CI-01 repository validation at source SHA `caea4019...`.
- **NOT_RUN** native Unity 6 EditMode/PlayMode, import, URP, PhysX, Windows, gameplay camera or actual collision. CI-02/03 fail at activation preflight; not test execution. AABB sampling does **not** prove full triangle-mesh nonintersection or physical contact.
- **PENDING** independent B01-60 review and B01-00/HQ manual art acceptance. **G2/CODEX_ACK/INTEGRATION/MERGE = NO**.

**Requested next action:** B01-60 independently inspect v001 vs v002 PNGs and source+hash manifest, repeat Blender QA if available, and report visual and topology defects to B01-00. No source edits without a fresh HQ return-for-rework decision.
