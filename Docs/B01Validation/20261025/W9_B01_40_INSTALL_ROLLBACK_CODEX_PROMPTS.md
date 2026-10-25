# W9 — B01-40: isolated import / rollback / Codex prompts

**Dispatch:** HQ-B01-20261025-W9-SOURCE-REPAIR. **This document is a gated preparation guide, not an integration instruction or handoff approval.** Receiver = Codex. Codex's live/unpublished worktree is strictly out of scope. MAPGEN-001 (PR #211) retains first **next unassigned** Codex slot; CAMERA, BUILD, RAC W9 repair does not displace it. No automatic Codex invocation.

## Verified exact package pins

| Package | Original native-RED commit (PR #212) | Corrected full-tree commit | Corrected stacked Draft PR | Full independent B01-40 SHA-256 manifest |
|---|---|---|---|---|
| CAMERA-001 R1.1 | `894d3488dedb69e2b8d89683d750bbdbfedf4e55` | `d48108f56d3fe0790ad715683e77b2de9219e4b1` | [#214](https://github.com/C8LyNn/RoboRougelike/pull/214) on [#134](https://github.com/C8LyNn/RoboRougelike/pull/134) | [CAMERA manifest](W9_GATE_CAMERA001_SHA256_MANIFEST.json) |
| BUILD-001 R1.2 | `d575b79e262ec9b65702608d6b6e28972da0d21c` | `489676414a3eb45fe70124098e3778a81d0aaff2` | [#215](https://github.com/C8LyNn/RoboRougelike/pull/215) on [#149](https://github.com/C8LyNn/RoboRougelike/pull/149) | [BUILD manifest](W9_GATE_BUILD001_SHA256_MANIFEST.json) |
| RAC-001 R3.1 | `6c1aa7c2351a29695e4288dddad2565cdec3682a` | `fd9a3f4dda08eaba56d9f74007bc35dc0809b67e` | [#216](https://github.com/C8LyNn/RoboRougelike/pull/216) on [#13 + #135](https://github.com/C8LyNn/RoboRougelike/pull/135) | [RAC manifest](W9_GATE_RAC001_SHA256_MANIFEST.json) |

All source commits and manifests are pinned. If any head changes, redo SHA-256, GUID, manifest, dependency, Edit/Play and gate checks at the **new immutable commit** before use. Producer manifest per-file SHA256 is currently null; B01-40's standalone immutable-source manifests provide independently calculated SHA256/lengths and must accompany a handoff or be reconciled into a corrected producer manifest. Source-side source path collision/manifest SHA1/bytes verification = PASS at the pins above; no ZIP binary has been independently extracted/hashed by B01-40. Producer's successful static CI ZIP hashes are **reported**, not a B01-40 verified replacement for file-level manifests.

## Gate prerequisites and safe import

**G0_SOURCE:** immutable complete source, every .meta paired, GUIDs, asmdefs, all source/manifest file hashes, full parent for stacked RAC and source license/provenance. **G0_TARGET:** read-only Codex ACK of exact live branch/head/worktree state, reserved paths, dependencies PHY #125 / OBJ #210, destination GUID collisions and ownership. Published QA reference OBJ #210 = `4c9236a4cdc27814ea2e7679872ec4a524150cc8`; it is **not automatically the current Codex integration target**. **G1_NATIVE:** B01-60's independent exact-source Unity 6000.3.14f1 compile + EditMode + PlayMode + package-specific manual/physics with raw XML/editor logs/exits. **G2_HANDOFF:** B01-00 explicit approval for this exact SHA after G0_SOURCE+G0_TARGET+G1, separate from Codex's later live-game integration. Until all conditions hold: `G2=HOLD / NOT_SENT / NOT_ACKNOWLEDGED`.

**Safe *test* rehearsal** in three distinct disposable target checkouts (do not modify a live Codex checkout):

1. Fetch the exact source branch/commit. Verify `git rev-parse HEAD` matches the manifest source_SHA after checkout; pin every source including the parent PR #13 for RAC.
2. Create separate temporary worktrees from a **confirmed published** read-only target snapshot (default for historic W9 RED comparison: OBJ `4c9236a4cdc27814ea2e7679872ec4a524150cc8`; obtain new Codex ACK before claiming G0_TARGET). Put logs outside worktrees.
3. Check the target directory for same-path overrides and scan serialized GUIDs **before copying**. Preserve any pre-existing shared parent .meta GUID. Never overwrite shared parent meta; RAC `Assets/RoboRougelikeMechanics.meta` is byte-identical to OBJ target and MUST remain target-owned. If a parent meta differs, STOP rather than overwrite.
4. Import **only one complete isolated package** including .meta and asmdef. CAMERA: `Assets/B01Packages/CAMERA001/**`, parent `Assets/B01Packages.meta` only when absent. BUILD: `Assets/B01Construction/BUILD001/**` and `Assets/B01Construction.meta` only when absent. RAC: full `Assets/RoboRougelikeMechanics/RAC001CheckpointRace/**` from #216 full tree, **never only child PR delta**, preserve target `Assets/RoboRougelikeMechanics.meta`. Docs/tools are QA attachments and not runtime Unity game replacements.
5. Independently verify every imported source file byte length and SHA-256 against the accompanying `W9_GATE_*_SHA256_MANIFEST.json`. Assert copied GUIDs are identical to manifest and distinct from other target GUIDs. Do not copy the source repository's global `Packages/`, `ProjectSettings/`, `Assets/Pack/`, `Assets/R3/`, `Assets/R6/` or `Assets/scene 1.unity`.
6. Unity batchmode `-quit -batchmode -nographics -projectPath` clean compile/import smoke with editor log **and process exit**; distinguish compiler errors in logs from success exit. If it does not compile, record FAIL + exact line and stop before EditMode. If compile succeeds run EditMode and PlayMode with non-empty test XML, actual test counts and exit codes. Unity Test Framework version 1.6.0, URP 17.3.0 on Unity 6000.3.14f1. Repeat negative tests and manual demo as below, preserving screenshots/video.
7. Close Unity; preserve evidence and restore/remove only the temporary worktree **after checking its changes and backing up non-git evidence**. No blanket `git clean -fd`, `git reset --hard` on shared branch, or force-delete of unknown files. For later Codex-only approved isolated integration rollback, revert *only* the recorded integration commits, remove the opt-in package adapter, preserve shared .meta, restore original controller/scene ownership, rerun impacted regression.

Illustrative PowerShell skeleton **for a disposable checkout only**, not to run on current Codex worktree:

~~~powershell
# From a verified clean repository, never from the Codex live worktree:
$SourceSha = '<choose EXACT corrected SHA from table>'
$PublishedTargetSha = '4c9236a4cdc27814ea2e7679872ec4a524150cc8'
git fetch origin
git cat-file -e "$SourceSha^{commit}"
git cat-file -e "$PublishedTargetSha^{commit}"
git worktree add --detach ../W9-PACK-SOURCE $SourceSha
git worktree add --detach ../W9-PACK-QA $PublishedTargetSha
git -C ../W9-PACK-SOURCE rev-parse HEAD
git -C ../W9-PACK-QA rev-parse HEAD
# Inspect source/target .meta and import only the appropriate complete isolated directory.
# Never overwrite an already-existing parent .meta. Verify hashes and GUIDs first.
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'
$Project = (Resolve-Path ../W9-PACK-QA).Path
$Evidence = (Resolve-Path .).Path
& $Unity -batchmode -nographics -quit -projectPath $Project -logFile "$Evidence\W9_compile.log"
$CompileExit = $LASTEXITCODE
# Examine W9_compile.log for compiler errors before EditMode.
& $Unity -batchmode -nographics -quit -projectPath $Project -runTests -testPlatform EditMode -testResults "$Evidence\W9_edit.xml" -logFile "$Evidence\W9_edit.log"
$EditExit = $LASTEXITCODE
& $Unity -batchmode -nographics -quit -projectPath $Project -runTests -testPlatform PlayMode -testResults "$Evidence\W9_play.xml" -logFile "$Evidence\W9_play.log"
$PlayExit = $LASTEXITCODE
# Validate non-empty NUnit XML and completed tests. Do not treat missing XML as PASS.
~~~

**Rollback rehearsal:** undo/import-delete just the package-owned copied subtree from `../W9-PACK-QA`, preserve target shared root metadata and baseline, verify `git status --porcelain`; after backing up logs outside, safely remove the disposable worktree using `git worktree remove` (without force, only once clean). If Unity generated untracked Library files, remove only those known-owned temporary outputs after operator review; never touch live checkout.

## CAMERA-001 — copyable Codex handoff prompt (PHASE 0 ONLY TODAY)

> ROLE: CODEX target-game integrator. HANDOFF PREPARATION ONLY, not approval. Review original PR #134 / native-RED `894d3488dedb69e2b8d89683d750bbdbfedf4e55` and corrected **FULL source** PR #214 at `d48108f56d3fe0790ad715683e77b2de9219e4b1`. Verify 37-file manifest `W9_GATE_CAMERA001_SHA256_MANIFEST.json`, 16 GUIDs and both corrected test asmdefs. Require B01-60 independent compiled Edit/Play results on identical SHA and B01-00 signed READY; absent these, STOP BEFORE IMPORT INTO LIVE GAME. Report current Codex branch/HEAD/dirty state, unpushed camera changes and P12/camera ownership; never dual-drive the active Camera. You may run only disposable exact-SHA package native QA. After future explicit G2 + scope ACK, integrate one selected geometry solver/rig adapter on Codex's OWN new isolated Draft branch, preserve P12 reticle and weapon aim, run 360° zoom/hull occlusion/walls Light+Heavy 720p/1080p Edit/Play, physical targeting/garage/combat regressions and Windows x64 smoke. Capture commands/XML/log/exits/screens/video and rollback commits; no merge/release. Package bug -> #66 B01-20 new SHA and B01-60 retest. MAPGEN #211 remains first next unassigned Codex package; do not supersede it.

## BUILD-001 — copyable Codex handoff prompt (PHASE 0 ONLY TODAY)

> ROLE: CODEX target-game integrator. HANDOFF PREPARATION ONLY. Original native-RED PR #149 `d575b79e262ec9b65702608d6b6e28972da0d21c`; corrected **FULL source** PR #215 `489676414a3eb45fe70124098e3778a81d0aaff2`. Verify 37-file `W9_GATE_BUILD001_SHA256_MANIFEST.json`, 17 GUIDs and corrected Edit/Play test asmdefs. STOP without exact SHA B01-60 independent Unity G1, B01-40 verified recipient-target G0 and B01-00 READY. Read target R3VehicleAssembly/R3GarageView/R3MountingGrid/P12, PHY/OBJ and potential visual PR #55 conflict; report active ownership, local changes and reserved paths. Source demo/tests may be run in a separate OBJ210 disposable checkout only. After future G2 in own isolated branch, map structural/axle/weapon/suspension typed mounting geometry, ensure wheel hub-only and gun-base-only contact, rotated XYZ 2x2x1 occupied cells, Core-connected fragments and real RaycastHit; preserve serial owner IDs, save/garage state, original wheel physics and economy. Execute native Edit/Play + manual Light/Heavy mount/garage and Windows x64 test, preserve logs XML/exit/visuals and revertable commits. On source defect stop -> #140 B01-20 then independent retest. No live host changes now; MAPGEN retains next unassigned queue slot.

## RAC-001 — copyable Codex handoff prompt (PHASE 0 ONLY TODAY)

> ROLE: CODEX target-game integrator. HANDOFF PREPARATION ONLY. RED source PR #135 full-stack on #13 `6c1aa7c2351a29695e4288dddad2565cdec3682a` failed native Unity CS1027; corrected **FULL source** PR #216 `fd9a3f4dda08eaba56d9f74007bc35dc0809b67e`. Inspect 37-file full SHA-256 manifest, 17 GUIDs (including the shared existing root .meta) and Editor/RAC001DemoSceneBuilder.cs trailing `#endif // UNITY_EDITOR`. Use **entire #13+#135+#216 source tree**; do not cherry-pick child diff. No Editor scene saved into the live game. Obtain B01-60 independent native Unity Edit+Play+physical demo PASS and B01-00 G2 + confirmed Codex target scope before any integration. Report actual target branch/HEAD, OBJ/race/activity ownership and unpushed work, else STOP. Disposable QA can test force-driven 150 kg Rigidbody floor contact, ordered checkpoints, wrong actor, multiple colliders, timeout, reset/restart, no reward duplication. After future approval, Codex implements opt-in activity hookup ONLY on own isolated branch; do not duplicate player movement, MAPGEN, OBJ rewards or checkpoint state. Run real wheel PHY regression and Windows x64 with XML/log/exit/screens, preserve rollback, no merge or release. Failure -> #8 B01-20 repair + B01-60 revalidation. MAPGEN #211 remains next.

## Final receipt contract

For each package return: `source_sha`, `producer_manifest_sha`, `B01_40_sha256_manifest`, `target_sha` and scope ACK, `G0_SOURCE`, `G0_TARGET`, `G1_NATIVE` compile/Edit/Play/manual with raw evidence, `B01_60 QA acceptance`, `G2 B01-00` signed receipt, `sender` and `receiver ACK`, `integration Draft PR` only after authorization, `rollback SHA`, test counts/exits, and any error. A GitHub comment, workflow static green or this prompt does **not** automatically wake Codex or authorize integration.
