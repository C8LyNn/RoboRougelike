# MAPGEN-001 — GPT OSS handoff guide (conditional)

**Package state:** DRAFT / NOT_READY_FOR_GPT_OSS until independent B01-60 QA and source freeze. **Do not integrate from this document alone.** Full branch and immutable HEAD available on the linked Draft PR; verify it against an actual B01-00 G2 receipt before use.

1. Obtain R1 from **the isolated Draft PR only**; inspect diff `Assets/B01Packages/MAPGEN001/**` + `Docs/B01/MAPGEN001/**`. Do not merge current `main`/`develop` or import PR #3.
2. In a copy/worktree with Unity 6000.3.14f1 and compatible URP, import both folders including all `.meta` GUIDs and `.asmdef`. Do not copy W8 run controller, shared scenes or package lockfile from this PR.
3. Open `Assets/B01Packages/MAPGEN001/Demo/MAPGEN001_Demo.unity`, Play, regenerate seeds, switch recipes and rigs, verify real collider contact. Run EditMode/PlayMode and record XML/exits. No claim of PASS without this.
4. For an isolated host experiment, create a GameObject `MapGenBuilder`, call `Generate(recipe, seed, config, vehicleProfile)`, pass `Start/Finish/Spawns/CheckpointDescriptors` as metadata to host. Host must decide if/when enemies or course checkpoints are instantiated; never let generated markers award rewards or mutate the run.
5. Do not replace `R5Arena.cs`, `R5RunPlan.cs`, `Assets/scene 1.unity` or `MAP-R6` by automatic copy. Plan the explicit adapter separately and protect serialized prefabs/vehicle state.
6. Rollback: deactivate/destroy generated root via `Clear()`, remove isolated opt-in host component and restore target worktree commit; no migrated state.
7. After B01-00 READY_FOR_GPT_OSS, send exact PR+HEAD+file manifest+independent QA to GPT OSS, obtain explicit ACK of target branch/worktree. GPT OSS owns later integration and game builds, not B01.

**Conditional integration prompt to GPT OSS:** "Inspect exact approved MAPGEN001 Draft PR HEAD and source hashes, reproduce Unity test suite in isolated worktree, integrate only via an opt-in host adapter into the existing R5 run without rewriting its economy, damage, enemies or garage, maintain CHL003 separation and report actual compile/tests/Windows build/manual drive QA with explicit FAIL/NOT_RUN. Never merge/deploy without owner approval."

## R2 candidate / integration hold
Follow-up R2 branch `feature/b01-mapgen001-r2-validation` **depends on** R1 Draft PR #99 HEAD `6dd21a72c6ec1590015042be6775a7bd0f57f3e1`. Do not import R1 and R2 as competing independent generators. Install **only the latest validated R2 tree** if B01-00 later authorizes G2, retaining all Unity .meta GUIDs, asmdef names and isolated `Assets/B01Packages/MAPGEN001/**` paths. Target game integration, actual Unity 6 compilation, and builds are GPT OSS responsibility.
Before import obtain `CODEX/GPT_OSS SCOPE ACK` for reserved game paths, verify actual commit/manifest hashes, review `MapGenRouteValidator` behavior (grid-based conservative pass). Run full EditMode/PlayMode suite including physical rover traversal and manual inspection. Keep the original R1 changes available for rollback. **Current authorization: HOLD, pending B01-60 independent QA and B01-40 package checks.**

## R3 package snapshot and operator-controlled handoff
- This is the **full stacked branch tree** `feature/b01-mapgen001-r2-validation` at [Draft PR #103](https://github.com/C8LyNn/RoboRougelike/pull/103), descended from Draft PR #99. PR #103's *14-path diff alone is NOT a complete install package*: use the complete branch snapshot, including R1 scene and .meta files.
- Obtain the FINAL immutable SHA from Issue #93 / B01-00's G2 receipt; compare with `git rev-parse HEAD` after checking out a **separate clean worktree**. Do not checkout/merge it onto an active Codex/GPT OSS game branch.
- Transfer only `Assets/B01Packages.meta`, `Assets/B01Packages/MAPGEN001/**` (including every .meta) and optionally `Docs/B01/MAPGEN001/**`. Do not overwrite any target project file without a manual conflict/GUID scan.
- Confirm Unity 6000.3.14f1, appropriate URP project, Unity Test Framework, standalone B01.MAPGEN001.Runtime and test asmdefs; no compile dependency on MAP-R6 PR #3.
- Re-run the R3 TEST_PLAN commands and MANUAL R3 12-trial matrix **before** writing any host adapter. Compare logs/XML and report exact SHA to B01-60. Then only with B01-00 permission, GPT OSS may bind Start, Finish, Spawns and CheckpointDescriptors into a new opt-in adapter; CHL-003 still exclusively owns state, rules and rewards.
- Rollback: call Clear(), remove the opt-in generated-root/host adapter from the test scene, revert copy only of B01Packages/MAPGEN001 and its .meta files to the target's prior worktree snapshot. No changes to gameplay state/prefabs were authorized by this producer.
- GPT OSS execution prompt after approval: "Verify the complete approved MAPGEN001 R3 branch at the exact G2 SHA and per-file hashes, import it into a temporary clean Unity 6000.3.14f1 worktree with GUID validation. Run all EditMode/PlayMode and manual 12-trial matrix, capture XML/logs/exits. Only then propose a minimal opt-in adapter to run/activity metadata. Do not duplicate CHL-003 checkpoint progression, overwrite W8 or merge before approval. Report SHA and rollback."
- Package status: `NOT_READY_FOR_GPT_OSS` until independent B01-60, B01-40, Unity evidence and owner G2 approval.
