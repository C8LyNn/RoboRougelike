# MAPGEN001_PACKAGE_GATE.md — B01-40 P0 independent package gate

**Audit snapshot:** 2026-10-24; **source under audit:** [Draft PR #103](https://github.com/C8LyNn/RoboRougelike/pull/103), immutable commit **e22906f9c973a632ca92815a964fadfbe1790665**. The original requested checkpoint was a2993a4cb09275864a04c6eaf8bc7b9f4dfb9946; PR #103 advanced **seven commits** and changed six C# source/test files. This audit pins e22906f exactly; it is not approval for later moving HEAD.

**Repository:** https://github.com/C8LyNn/RoboRougelike; **Issue:** https://github.com/C8LyNn/RoboRougelike/issues/93; **R1 PR #99:** https://github.com/C8LyNn/RoboRougelike/pull/99 at 6dd21a72c6ec1590015042be6775a7bd0f57f3e1; **R2 PR #103 base:** R1 HEAD; **main/develop baseline observed:** 5f06af81e2dd9d877e4300259bb3dd008cb9c9d1. **Recipient:** GPT OSS only after B01-00 G2; not Codex. No merge, build release, host integration or receiver ACK performed.

## Decision

**REWORK / HANDOFF BLOCKED.** G0=**FAIL (stale source manifest at current HEAD)**; G1=**BLOCKED (Unity NOT_RUN, independent B01-60 QA not received)**; G2=**BLOCKED / NO GO**. B01-00 has not issued a verified G2 authorization, and GPT OSS target branch/commit, worktree state and path reservation ACK remain UNKNOWN.

## Audited contents and static checks

- 43 tracked package files total: 20 .meta, 3 .asmdef, 7 C# (runtime 5; tests 4? see note), one .unity demo and 10 documentation files including existing manifest. **Correct count is 5 runtime C# + 4 test C# = 9 total C#**. Counts in this paragraph should use this correction: 43 total files, 20 .meta, 3 asmdef, 9 C#, 1 demo scene, 10 docs.
- All .cs/.asmdef/.unity source assets in the frozen tree have matching .meta paths; 20 inspected package GUIDs have valid 32-hex format and no intra-package duplication; scene m_Script GUID b01a000100000000000000000000000d refers to MapGenDemo.cs.meta. Exhaustive collisions with a *different/unpublished* target worktree cannot be proven from published Git alone; target Unity import/GUID collision scan remains required.
- The R2 delta from R1 is package-path-only. The additional seven commits from a2993a4 to e22906f modify only six package C# paths. No path overlap with the 109 changed MAP-R6 PR #3 paths or main/develop file paths. This is a PATH check, not a proof of semantic compatibility with unpublished code.
- Runtime source inspection: B01.MAPGEN001 namespace and B01.MAPGEN001.Runtime assembly has empty references; separate NUnit EditMode and PlayMode asmdefs use Unity Test Runner. The generator does not statically call MAP-R6, MAP-001, CHL-003, run controller, rewards, garage or AI. CHL-003 checkpoints remain descriptors only.
- Repository ProjectSettings/ProjectVersion.txt is 6000.3.14f1 (d68c3f99a318). Packages/manifest.json declares com.unity.render-pipelines.universal 17.3.0 and com.unity.test-framework 1.6.0. Versions and references inspected, but actual Unity compilation/import/build was NOT_RUN.
- New e22906f source adds guarded build cleanup, Regenerate API, camera tracking/yaw-only demo Rigidbody, 0.25m spine samples, 3x3 floor support and spawn-footprint checks, more negative EditMode/PlayMode source tests and arena cover jitter. These are static code claims pending execution.

## G0 source integrity blocker — manifest mismatch (P0)

R2 manifest Docs/B01/MAPGEN001/manifest.json has 42 Git SHA-1 path identities (all files except itself) and still advertises old a2993a4 content. At exact e22906f, **six entries are stale**. The 37 other entries still match tree blobs. This is a concrete integrity FAIL even though independent fresh SHA-256 values are supplied below:

- Runtime/MapGenBuilder.cs: ab5307ae189dd65f7e718f4138f4b10c80926ef5 vs manifest 04774de8e5bb9519fa2bef25d531a8866a92cd5c
- Runtime/MapGenDemo.cs: f6703d56c55562bdac0db9703ace82c99a442d78 vs manifest 85469c462144f51c61426f8f42dff544b9c0056a
- Runtime/MapGenPlanner.cs: fe2585d78d9e0c3e8f3c8575e6b751f7853c1bf6 vs manifest 6174d04886fbe5225c7f51e8489a363495bf2d44
- Runtime/MapGenRouteValidator.cs: 4e4bee443a60abb3b284e8c546203096095e12bd vs manifest 08ff3069740e53f573a97a64f1be31f448aeab11
- Tests/EditMode/MapGenRouteValidationTests.cs: ad921b55a1708d3d50b3555f21a075f1d603af04 vs manifest 4b9bbe4a6bc7cb56fd7cf0b7863d11453f92e185
- Tests/PlayMode/MapGenPlayModeTests.cs: a7d878428f41f3b9196f36f4cfb06ad8f6086b36 vs manifest 61a7ea41f7577666d16f60b3ef2aad22ccbbbcb6

Producer B01-20 must regenerate the source manifest for the new exact HEAD, include per-file SHA-256 and byte lengths, freeze the SHA, then request independent re-audit. SHA-1 Git object IDs in the producer manifest are NOT release SHA-256. No standalone ZIP/package artifact existed in verified evidence at audit time: ZIP URL, bytes and ZIP SHA-256 = **NOT_AVAILABLE / NOT_COMPUTED**. Do not invent a checksum for an uncreated archive.

## SHA-256 manifest — actual frozen GitHub source bytes

Method: fetched 43 text file contents by immutable Git ref via GitHub authenticated fetch_file(base64), decoded byte-for-byte, applied SHA-256 independently (algorithm cross-checked against canonical SHA256(abc) test vector), and recorded byte lengths. These digests are for individual repository files, **not** a ZIP, not a passing Unity runtime, and not a substitute for producer manifest repair.

| Source path | Bytes | SHA-256 |
|---|---:|---|
| `Assets/B01Packages.meta` | 172 | `c7b723f30e7c41b25011e4420275d34df5cb1f5e717af97d9a072935b4b70fed` |
| `Assets/B01Packages/MAPGEN001.meta` | 172 | `484655486f37cbbc034f1bdb92fa64ed240db78ef7a331be1d0c5179f78a0469` |
| `Assets/B01Packages/MAPGEN001/Demo.meta` | 172 | `2c1f59967ec7f14bd53d9bc8abc17e2976f0fea18e3e8af45ea65d33c8d7f71e` |
| `Assets/B01Packages/MAPGEN001/Demo/MAPGEN001_Demo.unity` | 1423 | `887f7705374556ac56bb455382e3f2fe6e422b228b2ebb56480d4fdbfe6cf064` |
| `Assets/B01Packages/MAPGEN001/Demo/MAPGEN001_Demo.unity.meta` | 155 | `1e90da696397e1bfe9b47bd7af7baf31f8cc6e93bf7512fd1fd7a5b24b43fe15` |
| `Assets/B01Packages/MAPGEN001/Runtime.meta` | 172 | `e4e251777ac1244103093215f40438cd8afc16ca8675942e56329ac22fe50a01` |
| `Assets/B01Packages/MAPGEN001/Runtime/B01.MAPGEN001.Runtime.asmdef` | 344 | `0cdab3502d7f033a6ba7f13213babf821594b81d4c2e054c769cb91cfb834345` |
| `Assets/B01Packages/MAPGEN001/Runtime/B01.MAPGEN001.Runtime.asmdef.meta` | 166 | `76ac5f68a274509a84acefdb7969e4437c9ff1ebda4fc5ef19ccdda27c50094e` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenBuilder.cs` | 4497 | `d6f411db921247e2cb84340a6664ee6875b33e54573513a5f975a0ca5d2e6f26` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenBuilder.cs.meta` | 243 | `0af403880361e9e56e004c24820cbb779eb12e41f60880e7fbfdb472e90ca024` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenData.cs` | 2521 | `4efac1ef18c43a67837a37b7dc43d4c6596e9cad1888d40130916f0aa3b00d53` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenData.cs.meta` | 243 | `01c89655cb8cab47d8c462019f6c9187cfdedadb7983e2d3ab404188c26db653` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenDemo.cs` | 6060 | `71a27b52da250a4b2cfaae4704160510dbf2c4f2014096801c66e1e13a73b725` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenDemo.cs.meta` | 243 | `47e555961768303b74a8e8b6267d3a01600dfb6a6c26ccbe90a430248acb5413` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenPlanner.cs` | 8649 | `e29c8b713c5ba08f3fbbce6f235b923b6f2f7cc37e8b4005f1ee10d6a82fdaf5` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenPlanner.cs.meta` | 243 | `f8534d9c0242203b1f5e6a6af4fa96771028d1d95dad247af56300b2fa2ca4c0` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenRouteValidator.cs` | 11238 | `4d87aa6a100f18a101c8b4be0e58fe72286111999fa5e96634348051a771ffc2` |
| `Assets/B01Packages/MAPGEN001/Runtime/MapGenRouteValidator.cs.meta` | 243 | `445a7533968b2db691bc74d92ff55f885dedd95296c347c8140aaa5e72d65ade` |
| `Assets/B01Packages/MAPGEN001/Tests.meta` | 172 | `3fb31cd84598accecf46915d4135c85629f87c5612c5013f08510d0ea3af5e95` |
| `Assets/B01Packages/MAPGEN001/Tests/EditMode.meta` | 172 | `9d6a85757f7c171d6381c695afcfcf11dfd34b25f25bb0d1cd98c2f02a3eb077` |
| `Assets/B01Packages/MAPGEN001/Tests/EditMode/B01.MAPGEN001.EditModeTests.asmdef` | 521 | `2adf3d0c513e3751386a0b48c15ce55f2a51ab5db2a9ef20155e31410dbb8ac4` |
| `Assets/B01Packages/MAPGEN001/Tests/EditMode/B01.MAPGEN001.EditModeTests.asmdef.meta` | 166 | `230069b38122383998b1fc477969b00fd511023565116b0709fca74c18b7ce36` |
| `Assets/B01Packages/MAPGEN001/Tests/EditMode/MapGenEditModeTests.cs` | 4270 | `7cdb51d22a2c00507696e971518a0610c24269c3ea58fc07ce4169919879021e` |
| `Assets/B01Packages/MAPGEN001/Tests/EditMode/MapGenEditModeTests.cs.meta` | 243 | `8e2ac20991ad14295039005bd8c5b0c54a756b83c454cc266f0a819700589080` |
| `Assets/B01Packages/MAPGEN001/Tests/EditMode/MapGenRouteValidationTests.cs` | 8575 | `55abe8434d22426558cab92d1478c3cce95967258cb153b017d4f419f1f80068` |
| `Assets/B01Packages/MAPGEN001/Tests/EditMode/MapGenRouteValidationTests.cs.meta` | 243 | `876b520913e6549d6494ac9c14af213ec88d9e7992320fac559361bf9a66a6fb` |
| `Assets/B01Packages/MAPGEN001/Tests/PlayMode.meta` | 172 | `88dd9cbd0f7b007ab143ea359213e1696e591361e040470d8180995353ff7c46` |
| `Assets/B01Packages/MAPGEN001/Tests/PlayMode/B01.MAPGEN001.PlayModeTests.asmdef` | 505 | `c59e2b273efd1d7d5519402fb20ab2cee7d79170fca66fda93102f107111e244` |
| `Assets/B01Packages/MAPGEN001/Tests/PlayMode/B01.MAPGEN001.PlayModeTests.asmdef.meta` | 166 | `326d450ea88bd6f9984e4ce178cf1e122cf6d08c95bd8a3ec4c45db014c9afd2` |
| `Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPhysicalTraversalTests.cs` | 3909 | `c03e0658f40488459b3e8b8511191581e91fa99002aeca7708ed41d558bcd4ab` |
| `Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPhysicalTraversalTests.cs.meta` | 243 | `a22a7e272539a06c87acd32acf2afc0fb3589475922d211e367081d4f6cee097` |
| `Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPlayModeTests.cs` | 6188 | `d79211f7dc129b3029cccc80da16d87e780d7387c42d22eff99b035d1b8ef981` |
| `Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPlayModeTests.cs.meta` | 243 | `0b287073130289cc68af687465d65f55f7c38e350962e769cf5ebd7bbc9e6fcb` |
| `Docs/B01/MAPGEN001/API.md` | 2673 | `9009af253e7bed187b2cce72b7512c6b8df581111761d9cf5e3a4ca75de0ce6a` |
| `Docs/B01/MAPGEN001/CHANGELOG.md` | 1392 | `9c9aef266aac61fc7eebb585b4d68f1f3f3a273604b16d7e5443a9ce2517c19a` |
| `Docs/B01/MAPGEN001/INSTALL_FOR_GPT_OSS.md` | 3167 | `1fd08696820afafc710016b388b7b66a935c9efc0d23c1f8a9f040abeb169350` |
| `Docs/B01/MAPGEN001/KNOWN_LIMITATIONS.md` | 2208 | `f1400569c9e1f579fe6e7e28c1489f33558b8da1ce7d4e317ce6d85e6e3a4b42` |
| `Docs/B01/MAPGEN001/QA_B01_60.md` | 553 | `1555a4a4dbc5ce1bc3c02fb05761804de30b4cbe320d09af86dea8896a92daa5` |
| `Docs/B01/MAPGEN001/README.md` | 1294 | `aa3ce7696f175d3b1b728119e4b87c227f41ad36119f0e24757a321e6f71cbea` |
| `Docs/B01/MAPGEN001/REUSE_DECISION.md` | 1848 | `b9c9c561b4d37b163f68fb10277c2bed6efefbc1703fe094448366386aeea8f8` |
| `Docs/B01/MAPGEN001/TEST_EVIDENCE.md` | 769 | `494e0ec9ab0fd10f3109c87a032a3e0fc9da16e00f05389d30a5ab5fb2034147` |
| `Docs/B01/MAPGEN001/TEST_PLAN.md` | 3362 | `2e62b7e12779ed1dc8013960af2b053fc0c2f87492f19d2fde16ce5b97648802` |
| `Docs/B01/MAPGEN001/manifest.json` | 6224 | `cda72caebe0c18fb271c64058d49f36ae466b45fc55ef7eb914d05a17c99591f` |

**Total checked package bytes:** 86234 across 43 files. The manifest.json hash is included; it was not included in its own Git SHA-1 file map.

## G1 evidence and independent B01-60 QA

- R1 GitHub Actions run 40119067459 (CI-01 Repository Validation) = **PASS (repository checks only)**. Runs 40119067340 (CI-02 Unity EditMode) and 40119067366 (CI-03 Unity PlayMode) = workflow FAILURE because Unity activation credentials preflight failed; native Unity test steps SKIPPED. Do not count these as Unity test FAIL or PASS: test execution = NOT_RUN.
- At e22906f, no accessible commit-associated Unity CI runs, NUnit XML, console exit results, manual driving video, screenshots or independent B01-60 exact SHA acceptance were established. C# compile NOT_RUN; Unity EditMode NOT_RUN; PlayMode NOT_RUN; 12 attempted source-coded Rigidbody traversal scenarios NOT_RUN; Windows x64 build NOT_RUN; actual game-vehicle drive NOT_RUN.
- Source test loop for 12 physics scenarios is one UnityTest. If a first scenario fails, later scenarios do not execute; independent QA should log results per (recipe, seed, rig) independently and retain failed seed IDs.
- Requested B01-60: inspect this exact SHA, attempt clean licensed Unity 6000.3.14f1 EditMode/PlayMode with process exit/ XML/Editor.log; independently reproduce blocked spine, invalid slope, turn radius, footprint, spawn overlap and regeneration; run >=3 manual seed cases per recipe with Light/Heavy and record physical motion, stalls, camera and colliders. If no runtime available, report BLOCKED/NOT_RUN with reason, not PASS. Source change -> new SHA -> fresh QA.

## G2 completeness and risk list

1. **P0** Manifest SHA-1 stale for 6 actual source files; producer should regenerate and append frozen per-file SHA-256+bytes.
2. **P0** Unity license/import/compile/EditMode/PlayMode/manual independent B01-60 evidence absent; G1 blocked.
3. **P0** GPT OSS exact target SHA/branch, reserved paths, GUID import collision scan and explicit receiver ACK UNKNOWN. No visibility into Codex/GPT OSS unpushed work.
4. **P1** No physical ZIP/artifact URL or ZIP hash. Immutable source branch + verified file hashes may be used as a candidate after manifest repair, but cannot claim ZIP integrity.
5. **P1** Existing INSTALL_FOR_GPT_OSS.md has conditional R1/R2 instructions and no current e22906f exact lock or signed acceptance receipt; update to reproducible pinned procedure and rollback.
6. **P1** Vehicle route proof is conservative XZ grid/footprint/spine, not a real dynamic vehicle/turn/traction certification. Arena/course difficulty calibration and independent manual tests remain required.
7. **P1** Target's existing same-path Assets/B01Packages.meta (if any) must never be overwritten; include exact path mapping and target GUID scan before selective import.

## Isolated import and rollback — proposed, NOT_RUN

**Safety boundary:** never modify the active Codex/GPT OSS checkout. Prepare a disposable clean checkout/worktree on the immutable source SHA; the following is a test plan, not evidence of execution.

```powershell
git fetch origin feature/b01-mapgen001-r2-validation
git worktree add --detach ../MAPGEN001_SOURCE_QA e22906f9c973a632ca92815a964fadfbe1790665
git -C ../MAPGEN001_SOURCE_QA rev-parse HEAD
# Use a separate licensed Windows machine with Unity 6000.3.14f1:
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'
$Project = (Resolve-Path ../MAPGEN001_SOURCE_QA).Path
& $Unity -batchmode -nographics -quit -projectPath $Project -runTests -testPlatform EditMode -testResults "$Project\EditModeResults.xml" -logFile "$Project\EditMode.log"
$EditExit = $LASTEXITCODE
& $Unity -batchmode -nographics -quit -projectPath $Project -runTests -testPlatform PlayMode -testResults "$Project\PlayModeResults.xml" -logFile "$Project\PlayMode.log"
$PlayExit = $LASTEXITCODE
# Inspect XML nonempty counts/failures/skips and Editor log; exit codes alone are insufficient.
```

**Selective target compatibility rehearsal only after scope ACK:** make another disposable worktree of *ACKed target SHA*, confirm it is clean, list target assets and GUIDs first, then copy only `Assets/B01Packages.meta` if target does not already own the parent meta; `Assets/B01Packages/MAPGEN001.meta`, `Assets/B01Packages/MAPGEN001/**` and `Docs/B01/MAPGEN001/**`, preserving every .meta and no package manifest/lock/settings replacements. Confirm no same-path overwrite, asmdef resolution, demo Play, test XML and repeatable script hash. Do not run this rehearsal on a live game worktree.

**Rollback in disposable QA worktree:** close Unity, preserve logs outside worktree, then `git worktree remove ../MAPGEN001_SOURCE_QA` from original repo, only after verifying no uncommitted user files; for a later GPT OSS integration, use its own isolated adapter commit and revert ONLY that integration commit/assets after collecting evidence. Do not use destructive unrestricted `git clean -fd` or reset main/develop.

## Queue entry — GPT OSS receiving lane

PACKAGE_ID=B01-MAPGEN-001; PRIORITY=P0; revision=R2 moving-head snapshot e22906f; producer PR=#103 (stacked #99); receiver=GPT OSS; source immutable commit=e22906f9c973a632ca92815a964fadfbe1790665; manifest status=STALE_6_PATHS; sha256 manifest=this report; zip_sha256=N/A; source dependencies=UnityEngine and Test Framework for tests; MAP-R6/MAP-001/CHL-003=NO_IMPLICIT_DEPENDENCY; receiver_target_sha=NULL; B01_60_QA=NULL; G0=FAIL; G1=BLOCKED; G2=BLOCKED; status=REWORK/HANDOFF_HOLD; SENT=NO; RECEIVER_ACK=NO. Only B01-00 may issue final READY_FOR_GPT_OSS after independent reruns.

**Role:** B01-40 INTEGRATION & BUILD, independent read-only package audit. No changes to producer source, main/develop, Codex/GPT OSS worktree, no source merge and no game release.
