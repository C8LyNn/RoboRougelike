# B01-40 / W9 — CAMERA001, BUILD001, RAC001 independent package gate

**Dispatch:** `HQ-B01-20261025-W9-SOURCE-REPAIR`  
**Report scope:** exactly three **full corrected source revisions**. Baseline failure evidence is Codex [QA-only PR #212](https://github.com/C8LyNn/RoboRougelike/pull/212), not a passing test report. No merge, game integration, release, deployment or direct access to unpublished Codex worktree. **MAPGEN-001 PR #211 stays FIRST next unassigned Codex package**; these three source fixes do not change queue order.

## 1. Immutable input and diff classification

| Package | Original source actually failed native Unity | Corrected exact source / stacked Draft PR | Diff original -> corrected |
|---|---|---|---|
| CAMERA-001 R1.1 | `894d3488dedb69e2b8d89683d750bbdbfedf4e55` #134 | `d48108f56d3fe0790ad715683e77b2de9219e4b1` [#214](https://github.com/C8LyNn/RoboRougelike/pull/214) on #134 | 6 changed paths: **2 test asmdefs**, installer, Codex prompt, W9 chronology, manifest. No C# or .meta changes |
| BUILD-001 R1.2 | `d575b79e262ec9b65702608d6b6e28972da0d21c` #149 | `489676414a3eb45fe70124098e3778a81d0aaff2` [#215](https://github.com/C8LyNn/RoboRougelike/pull/215) on #149 | 6 changed paths: **2 test asmdefs**, installer, Codex prompt, W9 chronology, manifest. No C# or .meta changes |
| RAC-001 R3.1 | `6c1aa7c2351a29695e4288dddad2565cdec3682a` #135 including #13 | `fd9a3f4dda08eaba56d9f74007bc35dc0809b67e` [#216](https://github.com/C8LyNn/RoboRougelike/pull/216) on #135+#13 | 5 changed paths: **Editor/RAC001DemoSceneBuilder.cs + one terminal #endif**, installer, Codex prompt, W9 chronology, manifest. No .meta changes |

Every corrected Draft PR is stacked on the failing full source, so DO NOT import only the patch. In particular, RAC requires full #13 + #135 + #216 history/tree; its additive scene is built through an Editor menu (no committed demo .unity), not the game scene.

## 2. Independent exact byte/Git/file identity checks

I fetched each immutable source file as GitHub base64 bytes, decoded bytes, calculated SHA-256 with independent JavaScript SHA-256 implementation checked against the canonical `SHA256("abc")` test vector, and reconciled each path's size and Git blob SHA-1 with Git tree and producer manifest. The resulting separate JSON SHA256 manifests cover producer manifest.json itself via independent out-of-band hash.

| Package | Complete source count | Actual Git bytes | .meta GUIDs | Paired asset metas | Git SHA1 + bytes against producer | Independent file SHA256 |
|---|---:|---:|---:|---|---|---|
| CAMERA | **37/37** | **56,753** | **16 unique** | PASS | **36/36 PASS**, self manifest separately accounted | **37/37 computed** |
| BUILD | **37/37** | **65,859** | **17 unique** | PASS | **36/36 PASS**, self separately accounted | **37/37 computed** |
| RAC | **37/37** | **58,146** | **17 unique** | PASS | **36/36 PASS**, self separately accounted | **37/37 computed** |

**Total:** **111/111 Git files**, **180,758 bytes**, **50 distinct .meta GUIDs** with no intra-package or cross-package duplicate, no missing .meta pairing on C#/.asmdef/.unity. CAMERA .unity MonoScript `b01c0000000000000000000000000009` equals Camera001Demo.cs.meta; BUILD .unity MonoScript `b01d000000000000000000000000000a` equals Build001Demo.cs.meta. RAC's `Assets/RoboRougelikeMechanics.meta` is a shared-parent file and is **identical (Git blob 6b6fc339...)** to published OBJ #210 target's parent meta; never replace it. Reported hashes refer to *individual source files*, not to a playable binary or game ZIP.

**Complete independent SHA256/byte manifests (immutable source SHA inside each file):**
- [CAMERA independent 37-file SHA256 manifest](W9_GATE_CAMERA001_SHA256_MANIFEST.json)
- [BUILD independent 37-file SHA256 manifest](W9_GATE_BUILD001_SHA256_MANIFEST.json)
- [RAC independent 37-file SHA256 manifest](W9_GATE_RAC001_SHA256_MANIFEST.json)

**Important handoff-format limitation:** the three producer manifests currently expose Git SHA-1 + length but keep per-file `sha256` fields null. This B01-40 QA branch supplies verified SHA-256 **as separate immutable-source audit sidecars**, but B01-20 should incorporate/reconcile these into the producer-side accepted handoff manifest before final G2; never claim the producer's own `sha256` fields already contain the values.

**Physical archived source packages:** producer [static CI run 40183207415](https://github.com/C8LyNn/RoboRougelike/actions/runs/40183207415), [artifact 12594982513](https://github.com/C8LyNn/RoboRougelike/actions/runs/40183207415/artifacts/12594982513), reports these **inner ZIP** digests (distinguish GitHub artifact wrapper ZIP, 82,780 bytes):
- CAMERA `CAMERA-001_W9_FIXED_FULL_SOURCE.zip`: 37 entries, 29,786 B, reported SHA256 `ed562c20435a4dfdc3e2ec94cab90965c93c27b3155f52e5d4e1c247de8e5b48`.
- BUILD `BUILD-001_W9_FIXED_FULL_SOURCE.zip`: 37 entries, 31,650 B, reported SHA256 `49f26653139808f4e070125d09b8605a3a736941bc8134299bafa0804b9dfecc`.
- RAC `RAC-001_W9_FIXED_FULL_SOURCE.zip`: 37 entries, 30,977 B, reported SHA256 `78ec4592ee4144504d6fb259addbba8cc5fd68685d9fc057f603d6a9cfec1a7a`.
These ZIP digest values are **producer CI reports, NOT independently rehashed ZIP bytes by B01-40**. Before recipient uses an archive it MUST check the extracted archive's exact inner ZIP hash and all file digests. Individual source SHA256 are independently verified here.

## 3. Static preflight RED → FIX results (not native Unity)

- CAMERA original EditMode and PlayMode each had direct `UnityEngine.TestRunner` and `UnityEditor.TestRunner` references plus `optionalUnityReferences: ["TestAssemblies"]`. Independent JSON test: **2/2 original FAIL**, **2/2 corrected PASS**; corrected `references` holds only `B01.CAMERA001.Runtime`, preserves optional TestAssemblies, Editor test platform constraint and original GUID. Runtime solver unchanged.
- BUILD original both test assemblies had same duplicated runner mechanism. Independent JSON test: **2/2 original FAIL**, **2/2 corrected PASS**; `references` now only `B01.BUILD001.Runtime`, TestAssemblies optional retained and meta/GUID identical; orphan Core graph and socket runtime untouched.
- RAC `Editor/RAC001DemoSceneBuilder.cs` original `#if UNITY_EDITOR` line 1 had **no closing #endif**; original static FAIL, fixed source has `#endif // UNITY_EDITOR` after closing namespace; balanced and source static PASS. This is exactly one C# preprocessor source line added, preserving the AddForce demo.
- Complete standalone source preprocessor directive scan: **18/18 C# files** checked (CAMERA5, BUILD6, RAC7), no unmatched `#if`/`#elif`/`#else`/`#endif`. CAMERA Demo contains 2 balanced `#if`/`#endif`; RAC Editor builder one balanced pair; remaining 16 files contain no such conditionals. JSON .asmdef parse and references inspected, runtime assemblies have no undeclared direct MAPGEN/OBJ/PHY dependency; RAC Editor asmdef is constrained to Editor and references RAC Runtime, two NUnit test asmdefs reference RAC Runtime.

Producer CI on [run 40183207415](https://github.com/C8LyNn/RoboRougelike/actions/runs/40183207415) reports 12/12 scanner fixture tests passing and all original RED/fixed GREEN static checks. This is supplemental source preflight, **not** Unity C# compilation/physically executed tests.

## 4. G0_TARGET — known published target versus actual unknown receiver

Published Codex receiver native baseline in PR #212 is OBJ-001 PR #210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8` (PHY #125 ancestry). Against its Git tree, CAMERA and BUILD use entirely new isolated paths; RAC existing shared parent `Assets/RoboRougelikeMechanics.meta` is byte-identical. PR #212's prior independent receiver audit records no target GUID collisions at the *original* source commits. **No .meta changed in repairs**, so this evidence carries to the same published target snapshot for those GUIDs. Our own independent check proves 50 package GUIDs are unique, but there is **no ability to inspect Codex's unpublished live worktree**. Thus G0_TARGET **PENDING**, not unconditional PASS. Require fresh Codex/owner ACK with actual working branch/head, dirty/unpushed state and reserved P12/R3/R6/OBJ/race paths.

Potential semantic collision: BUILD eventual R3 mount integration vs visual pilot PR #55; CAMERA future P12 camera ownership; RAC active OBJ activity/checkpoint/reward lifecycle. These are **host integration review requirements**, not evidence that isolated source already modifies shared game files. Only Codex, after G2, owns target integration.

## 5. G1_NATIVE — original RED evidence versus corrected current status

Codex [PR #212](https://github.com/C8LyNn/RoboRougelike/pull/212) executed licensed Unity 6000.3.14f1 on all three OLD sources. Three compilation attempts **FAIL, exit=1**, source errors exactly as described above, **zero NUnit XML**, no EditMode/PlayMode test cases executed. The corrected source refs are NEW but **native Unity compile/EditMode/PlayMode/manual driving HAVE NOT BEEN independently executed or evidenced**; B01-60 acceptance is NOT RECEIVED. A successful source linter or producer archive cannot be counted as independent Unity.

**Required B01-60 separate retests, each in its own throwaway published-OBJ comparison checkout (not Codex live worktree):** actual Unity 6000.3.14f1 compile/log/exit first, then EditMode and PlayMode with nonempty NUnit XML and counts, then:
- CAMERA: hull/reticle occlusion, 360 yaw/zoom, Light+Heavy collider geometry, walls/cover, 720/1080 screenshots, regression with active P12 camera ownership isolated.
- BUILD: axle hub-only, gun-base-only, rotated XYZ + 2x2x1 multigrid, orphan alive-but-Core-disconnected negative, actual Physics.Raycast, physical garage/controller checks, 720/1080.
- RAC: full parent and child tests, force-driven Rigidbody contacting collider floor, ordered checkpoints, wrong actor/multi collider, timeout, retry/reset, no early reward; Cube demo is not actual grounded PHY-wheel vehicle.
Record exact SOURCE SHA + QA checkout SHA + Unity version, raw editor/player logs, process code, XML executed/fail/skip and genuine video/screens. New error => producer correction at new source SHA + renewed B01-60 and B01-40 checks. If no licensed Unity or actual independent QA, `BLOCKED/NOT_RUN`.

## 6. Gate table / final handoff decision

| Gate | CAMERA R1.1 | BUILD R1.2 | RAC R3.1 | Decision rule |
|---|---|---|---|---|
| `G0_SOURCE` | **PASS_STATIC_WITH_B01_40_SHA_SIDECAR** / producer SHA256 fields need reconciliation | same | same | verified complete full source, Git blob+bytes, actual SHA256, .meta/GUID, asmdef/preprocessor; producer handoff manifest must include sidecar |
| `G0_TARGET` | **PENDING** | **PENDING** | **PENDING** | fresh Codex target scope SHA, live worktree ACK and target-specific import/GUID collision scan |
| `G1_NATIVE` | **BLOCKED / NOT_RUN** | **BLOCKED / NOT_RUN** | **BLOCKED / NOT_RUN** | B01-60 actual independent native Unity compile/EditMode/PlayMode/physical and logs on same SHA |
| `G2_HANDOFF` | **HOLD / NO GO** | **HOLD / NO GO** | **HOLD / NO GO** | needs all above plus B01-00 owner approval, current source pin, manifest, rollback |
| `READY_FOR_CODEX / SENT / ACK` | **NO / NO / NO** | **NO / NO / NO** | **NO / NO / NO** | no automated handoff from report |

No same-time game release, merge, main/develop mutation, host worktree patch or Windows release. No claim of native compile PASS. **Static package correction = verified; game integration eligibility = not achieved.**

## 7. Installation, rollback, ready receiver prompts

Full [W9_B01_40_INSTALL_ROLLBACK_CODEX_PROMPTS.md](W9_B01_40_INSTALL_ROLLBACK_CODEX_PROMPTS.md) includes exact source SHA for each, complete source scope, shared-meta preservation, separate disposable OBJ comparison checkouts, Unity batchmode command skeleton, file hash and GUID validation, logging requirements, 3 package-specific copyable Codex prompts and safe rollback. Existing producer INSTALL_FOR_CODEX and PROMPT_FOR_CODEX docs are present but their earlier R1/R3 body references are historically stale; W9 guard added at top, and **this B01-40 runbook supersedes ambiguous source pin text**. Codex PHASE0 read-only only until B01-60 G1, live target ACK and B01-00 G2. New target integration uses Codex's isolated branch only.

**Recipient queue order unchanged:** [MAPGEN-001 PR #211](https://github.com/C8LyNn/RoboRougelike/pull/211) first next unassigned Codex handoff; CAMERA/BUILD/RAC fixed candidates remain separate gated backlog. No automatic Codex notification or wake-up by adding GitHub files/comments.

## 8. Actions

**B01-20:** reconcile producer handoff SHA256 fields or attach immutable verified sidecar, preserve three fixed source pins; no extra runtime fixes without reproducible native FAIL.  
**B01-60:** independently execute exact-source licensed Unity and native/physical tests, report to #66/#140/#8 and #70, raw evidence.  
**B01-40:** repeat when moving heads or target ACK changes; verify final ZIP physically at receiver, target GUID/import, and handoff documents.  
**B01-00:** accept/deny final G2 only after all evidence and maintain MAPGEN queue ordering.  
**Codex:** no active-game import until explicit owner authorization.

**Current recommendation for all three:** `SOURCE_FIX_STATIC_VERIFIED / RECIPIENT_HANDOFF_BLOCKED`.
