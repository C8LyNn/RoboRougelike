# HQ-B01-MODELQA-S01 — prepared role prompts (NO UNAPPROVED AGENT IDs)

**Delivery status: PROMPT_PREPARED_NOT_SENT / awaiting manually confirmed ACK via operator/Model Lab coordinator.** Source issue #160. HQ's central SAQ numbering pending; DO NOT assign `B01-150`, `B01-3xx`, etc. No new permanent agent identity created in this document.

## Prompt for existing B01-30 / Model Lab interim coordinator
You coordinate quality review under B01-00, no self-appointed Queen. Inspect `Tools/ModelQA/v1/catalog.json` and `Docs/B01/MODELQA001/inventory-M0N.json` for all 70 pinned PR heads and 14 fix PR heads; preserve original factory claims from PR #35. Track three initial cases: M02-001 (old vs ring-hub stale OBJ/GLB), M05-002 (hinge sweep 755→0 broadphase positives), M01-003 (rail/shoe source but no committed mesh). Assign preparation of comparison artifacts to EXISTING named Model Factory M01, M02, M05 threads only after confirming operator delivery and ACK — no numerical new agent IDs. Each subtask uses model_id+original PR/fix PR immutable SHA. Provide ISSUE comment `ACK/START/CHECKPOINT/QA_RESULT/REWORK/CLOSEOUT`; if no actual agent ACK record WAITING. Do not run model factory regeneration in other workers' branch unannounced.

## Existing M02 producer thread (without new agent ID)
M02-001 only: inspect original #40 HEAD `5063e64f45ae396d99bb233b3b1d4fea98e28ecf` and fix #50 HEAD `ca4007606c67a9e0deadf43af8ad1bb59800bbb5`. Confirm actual GLB/OBJ old hashes unchanged in fix; redo generator in isolated rework branch and commit or durably archive regenerated OBJ+MTL+GLB/FBX plus SHA256. Verify shaft bore 86mm and reference shaft 68mm via REAL exported mesh topology, not only generator constants. Run independent source/export parity, Blender animation and 6-view renders; retain original FAIL evidence. DO NOT import to Unity game. Submit Draft rework PR and report all real tests / NOT_RUN.

## Existing M05 producer thread
M05-002 #46→73: independent run `modelqa.py` on immutable head + Blender evaluated actual animated geometry from ±22° at keyframes and fractional samples; original detects hinge/panel overlap, fixed sampled conservative AABB source already 755→0, but verify BVH collisions on real GLB; attach negative and positive scene cases. Load a Blender file with an unrelated sentinel cube; exporter must not delete or export sentinel. Export corrected animated GLB, .blend and 6 view + 5 poses, attach hashes and screenshot/geometry tables. Any visual/physics claim without Blender evidence = NOT_RUN.

## Existing M01 producer thread
M01-003 #52→102: repair original disconnected guides (46mm X, 33/41mm gaps), fixed source contains guides 230mm X, moving shoes; obtain actual CI OBJ binary from run 40120310833 and verify immutable byte SHA against regenerated source. Complete Blender-generated animated GLB/FBX and validate moving shoes contact guides through entire 115mm slide, especially intermediate frames; real topology+mount/pivot+render. No conflate CI artifact with committed asset or pass of Unity. No edits to weapons game code.

## Existing B01-40 independent provenance auditor
Inspect all 70 original PR HEAD and 14 FIX tree manifests from this QA branch; independently compute SHA256 on downloaded actual OBJ/GLB/FBX and confirm which CI artifacts expire or require permanent storage. Verify tool source/runner and evidence script cannot mutate external sources. Mark stale vs unknown distinctly. No G2 asset release from metadata alone, no Codex handoff.

## Existing B01-60 independent visual/mechanical QA reviewer
Separate from model producers. Re-run first 3 at exact SHA, old FAIL/fix PASS where supported, and all 11 `test_modelqa.py` auditor unit tests. Validate sampled intermediate hinge angles and AABB/narrowphase uncertainty; Blender sentinel and 6-view manual renders if Blender available. Report actual CLI/process/xml/outputs SHA256 and discrepancies; if absent Blender/Unity, status BLOCKED, not PASS.
All tasks return to Model Lab coordinator → B01-00 → HQ. New identities only when HQ confirms SAQ central numbering.
