# W12 MAPGEN R3.4 source handoff — B01-60 + B01-40, NO G2

Source parent R3.3 PR#225 exact `7334f3b1d8cd3a976098faa4e683f4de3d9a592f`.
Producer Draft PR#235 branch `fix/b01-mapgen001-w12-r34-cube-urp-20261025`.
**Read the exact immutable SHA from producer's final PR#235/Issue#93 receipt; do not use a moving head.**

Recipient B01-60: independently validate native Unity 6000.3.14f1 on a disposable checkout containing FULL stacked #99+#103+#211+#225+#235 source. Read TEST_EVIDENCE_R34.md and #232 RED. Run new flat control and 12 separate physical cubes, compile/Edit/Play raw logs and XML, and minimal **Windows Player URP without overrides** for both recipes with 720p/1080p screenshots plus regeneration/material lifecycle. Recheck real R6 Light/Heavy wheeled matrix in disposable OBJ #210/PHY #125 target, not Codex active worktree. Submit table with actual PASS/FAIL/NOT_RUN per case and exact source/target SHA; if blocked, say NOT_RUN.

Recipient B01-40: full source file bytes/SHA256 and paired .meta shader asset/Resource directory, GUID uniqueness within source and current recipient target; deterministic inner ZIP SHA256/entries and manifest; check shared parent `Assets/B01Packages.meta` GUID conflicts in recipient and **do not overwrite existing parent GUID**. Confirm no edits outside owned producer MAPGEN Assets/Docs/Tools/isolated CI, code assembly references, dependency on `com.unity.render-pipelines.universal` for custom shader compilation. No ProjectSettings, R3, R6, Pack or game changes. Verify resource asset is genuinely included in resulting Windows Player (not just present in Git). Install source only for QA, not production.

QA recipient remains CODEX *next unassigned task only after* existing task and exact target/scope ACK, B01-60 G1, B01-40 full G0 and B01-00/HQ G2 GO. Historic GPT OSS handoff superseded for this package. DO NOT MERGE OR IMPORT TO ACTIVE WORKTREE.

Rollback: in disposable QA checkout revert R3.4 only back to pinned R3.3 or remove R3.4 imports preserving all parent `.meta` GUIDs; if Codex later integrates after approvals, revert only dedicated adapter branch commits and restore current gameplay, keep source audit archived.
