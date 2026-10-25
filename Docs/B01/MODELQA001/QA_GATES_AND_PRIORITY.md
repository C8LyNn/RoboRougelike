# MODEL-QA-AUTO-001 — failure categories and action queue

## Independent evidence levels
- **Q0 / INVENTORY:** exact parent/fix PR sha, generator, committed binary presence, real source/artifact hash and importer constraints; `ARTIFACT_NOT_ACCESSIBLE` if CI zip expired/not retrieved. Already executed on metadata for 70, **not** physical mesh of all 70.
- **Q1 / MESH:** parse real OBJ/GLB/FBX triangle buffers, finite vertices, degenerate/nonmanifold edges, winding, UV/materials, components; compare asset revision to generator at SHA; must not infer from authored QA JSON flags.
- **Q2 / KINEMATICS:** Blender evaluated depsgraph world meshes at keyframes and adaptive midpoints, broadphase AABB followed by BVH/triangle narrowphase. Annotate expected relations WELD, AXLE, SLIDE, FREE and intended penetration vs accidental clash; one strict global no-overlap rule is invalid for mechanical welded parts. Include scene sentinel and assert non-destructive export.
- **Q3 / VISUAL:** six ortho sides plus 4+ animated poses, turntable shaded/material/normal and clipping debug; human compare to source and baseline. CLI R1 supplies geometry-only SVG wireframes, not shader/render PASS.
- **Q4 / OPTIONAL UNITY:** separate temporary Unity asset QA project after HQ authorization; scale metres, +X axle, Y-up Z-forward conventions vs export flags, pivots, sockets, 0.55m target suggested grid and animation; no import into game. Unity not executed.
- **Q5 / INDEPENDENT GATE:** second reviewer confirms exact model revision binary SHA256 and all results. Missing artifacts cannot be promoted.

## Severity and retest
`BLOCKER_PROVENANCE`: missing/old export, unknown SHA or absent source needed for reproduction.
`P0_FUNCTIONAL`: blocked axle bore, destroyed scene on export, impossible mechanical mount, confirmed no-contact support.
`P1_MOTION`: animated intersections, negative clearance, wrong pivot or scale, misleading endpoint-only full turn.
`P2_PRESENTATION`: shading, detached decorative piece, silhouette or material issue after mechanical QA.
`UNCERTAIN`: sampled AABB alarm without narrowphase; source-only fix without actual binary.
Retest every new generator version and regenerated OBJ/GLB/FBX, record ORIGINAL_SHA/FIX_SHA/ASSET_SHA256 and truth table OLD_FAIL/NEW_PASS/BINARY_PARITY. Never retest against moving branch name.

## Evidence-driven ranking
1. **M02-001 / PR40→50** — `P0 BLOCKER_PROVENANCE`: correction uses ring hub, committed OBJ/GLB identical to broken original. Regenerate full binary and independent bore QA before promotion.
2. **M01-003 / PR52→102** — `P0/P1 MISSING_DELIVERABLE`: source has guide/slider shoe correction, neither checked PR version commits static or animated mesh; verify CI artefact SHA and real moving GLB, rail/shoe sliding contact across full 115mm.
3. **M05-002 / PR46→73** — `P1 MOTION_RETEST`: original real-sourced sampled AABB positive 755, fixed 0 at half-degree; static OBJ changed, but need Blender evaluated hinge interpolation/GLB and preservation of sentinel foreign scene.
4. **M03-003 / PR51→92** — invisible SNAP markers previously; no committed exports in original/fix checked PR; first deliver actual binary then snap visible/physical contact test.
5. **M04-001 / PR37→47** and **M04-002 / PR42→58** — previous shaft-bore/rotation issues, no committed mesh exports in checked PR revisions; require actual artifacts, 360-degree non-collapsed quaternion interpolation and physical rotating hardware test.
6. **M02-014 / PR148** — preexisting analytic gear clearance, no committed OBJ/GLB in original PR inventory. Needs independent BVH and binary/QA claim verification; no failure proven.
7. **M05-014 / PR145** — source-only in Git inventory, evidence of Blender binary to be verified; do not label defective mesh without file.
Remaining 63 models **UNRANKED_GEOMETRY** until mesh proof; prioritize missing binary & functional dependency ahead of subjective aesthetics.

## 14 fix pair Git materialization summary (separate metadata)
- Changed committed mesh between baseline/fix: M01-002, M02-002, M02-003, M03-001, M03-002, M05-002, M05-003 (**7**).
- Unchanged committed mesh: M01-001, M02-001, M05-001 (**3**); source regeneration semantic check needed.
- Neither fixed nor original PR has a committed mesh export: M01-003, M03-003, M04-001, M04-002 (**4**).
Numbers from five `inventory-*.json` tracked trees. **Do not conflate absent Git files with absent CI artifacts.**
