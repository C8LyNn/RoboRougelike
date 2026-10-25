# W9 receiver validation — CAMERA / BUILD / RAC

Dispatch `HQ-B01-CODEX-W9-20261025`. **Allowed native diagnostic phase executed; production integration remains on HOLD.** No new gameplay/Windows candidate is delivered by this QA-only branch. Six original Git-visible source states and the four published W7/W8/B01/OBJ001 archives match their initial HEAD/clean states and published hashes; see baseline-preservation.json. Ignored local caches are outside that assertion.

## Target and ownership
Target PR210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8`, including PHY125 `916654634d1bfdc2bbc14e64b9eeac856ede1fa8`. Target Assets tree `f419cfec0d65eb512c62213ce509265da0861319`; this report branch has **zero Assets/Packages/ProjectSettings diff**. Baseline results1021/1021 Edit,401/401 Play and Windows/input defense victory are inherited verified OBJ evidence, not rerun results for these new packages.

Initial registered worktrees7/7 clean, no unpublished changes. Native worktree tool returned Not a git repository in projectless Desktop; separate manual sibling checkouts were used without copying Library or changing other checkouts. All170 open PRs and their complete changed-path sets were inventoried; every remote branch resolved. No open PR changes P12_PipelineSafeCamera.cs. Future BUILD changes to R3 assembly/garage overlap visual pilot PR55; legacy R3/R6 PRs are ancestry/source references, not new approvals. Full inventory: open-prs.json/open-pr-paths.json.

## Actual native results
| Package / immutable producer source | Isolated checkout/capsule | Native compile | EditMode | PlayMode / physical / visual |
|---|---|---|---|---|
| CAMERA001 PR134 `894d3488dedb69e2b8d89683d750bbdbfedf4e55` | RoboRougelike-W9-Validation, `qa/rrl-w9-b01-native`, original capsule `ee0aa45` | **FAIL, exit1** | **NOT RUN**, no XML;5 methods including840 proposals | **NOT RUN**;3 methods, real Light/Heavy/tall/wide/asymmetric,360/zoom/pitch/walls/reticle and720/1080 |
| BUILD001 PR149 `d575b79e262ec9b65702608d6b6e28972da0d21c` | RoboRougelike-BUILD001-Validation, `qa/rrl-build001-native`, `4d4465b3bc2775f7e5a7912af74044bdbf4df9d0` | **FAIL, exit1** | **NOT RUN**, no XML;11 authored methods | **NOT RUN**;2 methods, actual socket/XYZ/multi-cell/orphan/wheel/garage/save cases |
| RAC001 PR135 `6c1aa7c2351a29695e4288dddad2565cdec3682a` + entire PR13 parent | RoboRougelike-RAC001-Validation, `qa/rrl-rac001-native`, `52222a7e3c9f1c25c00df3cb768757773c979eb4` | **FAIL, exit1** | **NOT RUN**, no XML | **NOT RUN**; parent/new demo, actual R3/R6 grounded race, checkpoints/timeouts/multicollider/wheel loss/restart/rewards/OBJ |

These are **three actual licensed Unity6000.3.14f1 compilation attempts**, not three executed NUnit test cases. The runner stopped before PlayMode after each compile failure; repeating the identical broken compilation was avoided. No demo screenshot, actual wheel race or new Windows build exists, and none is marked PASS. Raw retained process handles confirm exits. Public Editor logs omit machine identifiers; complete originals remain in ignored local Artifacts/Local.

Source integrity alone passed: CAMERA35, BUILD35, RAC34 manifest entries match source Git blobs, bytes and checkout bytes, with computed SHA256 and no GUID collisions against the target. This is **receiver audit**, not independent B01-40/B01-60 certification. Entire RAC parent/R3 source was used, not incremental PR135 alone. Original source files were not patched.

## Reproduced defects and producer next actions
1. CAMERA001: both test asmdefs declare UnityEngine.TestRunner/UnityEditor.TestRunner explicitly and via optionalUnityReferences/TestAssemblies. Unity rejects duplicate references. B01-20 must publish a new immutable source choosing one reference route; no native runtime geometry conclusion is possible yet. [Report](CAMERA001_NATIVE_QA.md), [Issue66 receiver ACK/repro](https://github.com/C8LyNn/RoboRougelike/issues/66#issuecomment-6395860515).
2. BUILD001: same duplicated runner references independently reproduced without CAMERA installed. Existing orphan-support source correction remains unexecuted. [Report](BUILD001_NATIVE_QA.md), [Issue140](https://github.com/C8LyNn/RoboRougelike/issues/140#issuecomment-6395950632).
3. RAC001: Editor/RAC001DemoSceneBuilder.cs opens #if UNITY_EDITOR and reaches EOF without #endif; actual CS1027 at67:1. B01-20 must publish corrected immutable R3.1, then execute native tests including a real grounded host; cube demo propulsion must never replace game drive. [Report](RAC001_NATIVE_QA.md), [Issue8](https://github.com/C8LyNn/RoboRougelike/issues/8#issuecomment-6395951065).

No suggested correction is claimed retested or fixed. Per package failure protocol, no competing implementation, original-source rewrite or live-host patch was introduced. The diagnostic tool reuses existing CI XML validation so empty/missing results cannot count as success.

## Gates and continuation
Latest reviewed source/issue receipts: G0 source provisional/local integrity, independent G1 unconfirmed, **G2 HOLD and no B01-00 READY_FOR_CODEX** for each exact source. Receiver ACK was delivered to66 and [inbox79](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6395860935); this is not an approval. Next: producer source revisions → independent B01-60 native retest → B01-40 target/GUID scope → B01-00 G2. Only then create separate integration branches and execute full six-stage/OBJ/grounded drive/Core recovery/parts/projectiles/loot/repair/garage/restart/camera regression and Windows build.

MAPGEN remains assigned elsewhere; RUN-SAFETY loaner policy and model library are not imported. Existing automation-attached C0000005 remains separate/unfixed. New human/manual-input playtests are NOT RUN here because compilation never completed. This phase closes with actual evidence and producer feedback, not a gameplay-ready declaration.

## Local evidence review
One fresh read-only whole-branch review:0Critical,0Important,2Minor. This is not B01-60 acceptance. Deferred minors: the CAMERA/BUILD reproduction should explicitly copy the reviewed runner into a clean BASE checkout before invoking it (direct Unity invocation is also recorded in Issue66); and the open-PR path inventory should retain resolved head/base/merge-base SHAs for historical reruns after remote branches advance. These documentation/audit limitations do not alter the actual retained Unity failures. The full execution ledger records every declined-to-judge disposition.
