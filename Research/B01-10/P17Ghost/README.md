# B01-10 · PASS17 WAVE06 · GHOST-001/R1 — training ghost for CHL-003

**Product:** an optional skill-training feature that records/replays the physical *pose* of one attempt and compares Light/Heavy personal-best attempts at each of four original CHL-003 gates. This is original executable C# gameplay-support code, **not** another route/arena editor, run score engine, content pack, score formula, physics driver or duplicate of B01-20's course logic.

**Single owner:** B01-10 (game design & balance). **Upstream single owner:** B01-20 CHL-003 (do not modify their code). **Reviewers:** B01-40 source G0, B01-60 independent C# QA, B01-20 interface owner, B01-00 handoff policy. **Codex:** busy; no ACK, no authorized integration, no merge.

## Actual gameplay feature and API

\`GhostCore.cs\` contains actual portable, deterministic C# types with no UnityEngine dependency or shared game references:

- \`GhostRecorder(courseId, rigId, requiredCheckpoints, actualStartPose)\` opens **one** physical attempt, recording real host-supplied world positions/heading.
- \`Tick(unscaledDelta, paused, actualPose)\` samples at approximately 10Hz rather than allocating data every frame, accumulates only non-paused time, bounds each delta to 10 sec and a whole attempt to 15 minutes. **Tick never applies Rigidbody forces or controls vehicles**.
- \`Checkpoint(zeroBasedIndex, actualPose)\` records ordered checkpoint splits, rejects skips and repeats. A completed exported track also records RequiredCheckpoints; imported JSON cannot claim victory after fewer than the configured gates. **It must only receive checkpoint events already accepted by the real CHL-003 trigger/controller**. The library does NOT validate Unity colliders or actor identity.
- \`Finish(actualPose)\` refuses early finish, closes the recording and supplies a completed immutable-by-copy \`GhostRun\`. \`Fail(actualPose)\` closes a non-completing run, never a valid personal record. Duplicate finalization/ticking is rejected.
- \`GhostMath.At(completedRun, seconds)\` supplies a detached 3D playback pose, interpolating xyz and taking the shortest yaw arc across the 0/360 boundary. It only computes pose; Codex (if authorized) may render a translucent non-colliding ghost. **Do not turn this output into a physics vehicle or collision actor**.
- \`GhostBestBook.Submit(completedRun)\` stores a defensive deep clone of the fastest completed attempt per course+vehicle rig. A failed attempt, duplicate/equal best or slower completion cannot replace it.
- \`GhostMath.Compare(reference,trial)\` computes actual signed checkpoint split deltas and finish-time delta for **two completed attempts on the same course and matching gate count**, including cross-rig Light vs Heavy. No fixed "Heavy always slower" balance assertion, no equipment/credits/reward effect.
- \`GhostMath.Validate\` rejects NaN/Infinity, malformed timestamps, missing samples, invalid positions, missing configured checkpoints in a forged completed track, checkpoint drift, impossible time, uncontrolled length. Identity is separate by course and rig, case-sensitive. Records and best query results are deep copies.

**No persistent game save changes**: best book is in-memory, rebuilt or reset when the host decides. No monetization, economy, meta progression, combat, CHL-003 controller, R1, R3, R5, scenes or Unity project files are edited by this branch.

## Real executable and tests

\`\`\`bash
dotnet build Research/B01-10/P17Ghost/Ghost.Runner.csproj -c Release --warnaserror
dotnet run --project Research/B01-10/P17Ghost/Ghost.Runner.csproj -c Release --no-build -- --test
dotnet run --project Research/B01-10/P17Ghost/Ghost.Runner.csproj -c Release --no-build -- --demo
\`\`\`

The original source \`Program.cs\` runs **62 named positive/negative C# checks** including 100 extra deterministic synthetic motion traces, plus 2 synthetic example rigs and original JSON export. Output explicitly says **SYNTHETIC**, never live CHL-003 telemetry or native Unity/PhysX proof. \`--demo\` provides data for real game preview or design review; it is not a claim of playable Unity demo.

The dedicated isolated \`.github/workflows/b01-10-p17-ghost.yml\` compiles and runs the real same original C# under .NET8 on **Windows 2025 and Ubuntu 24.04**, uploads real test logs and demo JSON, negative command exit2. Only count actual PASS once the CI run completes; native Unity remains **NOT_RUN** until a licensed editor imports this code and tests real rig physics.

## Scope and Codex integration after approval

**Producer owns ONLY** \`Research/B01-10/P17Ghost/**\` plus one dedicated, unique GitHub workflow. **B01-20's** [CHL-003 original owner PR #285](https://github.com/C8LyNn/RoboRougelike/pull/285) stays unchanged; **B01-10 earlier #401/#357/#344 remain unchanged**. Existing issue [#91](https://github.com/C8LyNn/RoboRougelike/issues/91) requires 4 ordered actual checkpoint gates and two distinct real rigs. CHL-003 native Physics and Rigidbody always own real results; the ghost does not invent wins.

**Copy-ready Codex / B01-20 handoff (ONLY after B01-40 G0 and B01-60 QA):**

> Package B01-10 GHOST-001/R1 from the exact verified PR head and \`GhostCore.cs\` blob in MANIFEST. In an approved disposable Unity 6000.3.14f1 worktree, copy \`GhostCore.cs\` to a **new non-colliding unique** \`Assets/B01TrainingGhost\` folder with an authorized stable .meta GUID and compatible asmdef. Do not cherry-pick the producer branch wholesale. Wire an opt-in recorder only to B01-20 CHL-003 **already validated physical Rigidbody motion** after its Ready → Running transition: sample authoritative Rigidbody world pos and yaw with unscaled delta while not paused; call \`Checkpoint(i, pose)\` only *after* CHL-003's controller has accepted CP i. Call \`Finish\` only after true Completed; \`Fail\` for fall/timeout/Retry/Cancel. On retry, create a new recorder instance and keep any completed best only by player choice. Render a cloned pose on a non-colliding, trigger-free visual ghost (no Rigidbody/AI/pickups) and show per-checkpoint split deltas for two physical rigs. Validate with a real Unity demo: Light and Heavy traversals through four true gates on the same course; repeated pause/fail/retry; zero ghost-generated collision triggers; no changes to CHL reward/economy; real native EditMode+PlayMode logs and two-rig telemetry. Confirm any course/layout change invalidates old ghost frames, including variants/seeds, by composing a course identifier from the exact native layout version. Native G1 and visually validated ghost are **NOT_RUN** in this producer PR. Rollback: remove only newly installed ghost script/meta/authorized visual adapter, restoring original CHL scene and code bytes. NO merge until HQ.

## Known risks — explicit blockers

- The source package is not yet imported in Unity; Editor compile/PlayMode/PhysX/Human visual quality = **NOT_RUN**. A compiled .NET8 source test is not a native Unity check.
- No claim that wall-clock unscaled delta alone is authoritative game time: CHL-003/B01-20 must supply it and pause/scene-transition filtering; dropped frames are intentionally downsampled and interpolation may visually clip sharp jumps. Ghost is purely visual.
- Recorder does not check actor IDs, trigger provenance, world bounds against course colliders or physical reachability. Its \`Checkpoint\` input is trusted only after B01-20 validates physical actor crossing.
- No replay sharing, cloud saves, gameplay AI ghost, real ghost opponent, Unity .meta GUID production, or cross-run economy rewards. Sample values are synthetic.
- Existing B01-10 RUNSCORE001 stays under independent QA review and is not claimed SOURCE_G0_PASS; new branch is separate and does not invalidate its immutable SHA.

**Acceptable state until independent review:** \`PRODUCER_CSHARP_TESTED / B01-40_G0_PENDING / B01-60_QA_PENDING / CHL003_OWNER_ACK_PENDING / CODEX_ACK_PENDING / NATIVE_UNITY_NOT_RUN / NO_MERGE\`.


## PASS17 WAVE07 — GHOST-001/R2: real opt-in Unity URP visual + O(log N) playback

**One enhancement to the existing Draft PR #422, NOT a second competing PR.** Its prior W06 \`GhostMath.At\` performed full \`GhostMath.Validate\` over every frame on *each visual request*, making playback O(N) per frame. W07 adds actual C# \`GhostPlayback\`: on \`new GhostPlayback(completedRun)\`, the source track is validated once and deep-cloned (even hostile caller edits to the original frames cannot later affect replay); \`Seek(t)\` uses a private, guarded O(log N) binary search over the validated immutable copy, without repeatedly validating the entire track. Direct public \`GhostMath.At\` still validates **every untrusted input** to fail closed. \`Seek\` returns fresh pose objects, never internal mutable frames. \`GhostPlayback\` rejects failed attempts as a reference.

**Working authored Unity opt-in visual source:** new [\`GhostVisualProxy.cs\`](GhostVisualProxy.cs). It is compiled *only in Unity* under \`#if UNITY_5_3_OR_NEWER\`, and uses only \`UnityEngine\` / UnityEngine.Rendering + the original portable \`GhostPlayback\`. It creates an original procedural transparent 3D URP arrow \`Mesh\` + \`MeshRenderer\` + \`MeshFilter\` at runtime; it never creates a \`Collider\`, \`Rigidbody\`, trigger, weapon target, AI agent, reward or attack hitbox. It **refuses attachment to a non-empty/parented physical vehicle**. Method \`Bind(GhostRun)\` accepts only a completed validated track, \`Seek(float elapsed)\` receives a supplied playback clock, \`Hide()\` turns off rendering. The host still owns authoritative CHL checkpoint, pause/retry and real physics. Missing URP Unlit shader fails closed rather than claiming a visible ghost.

**Exact opt-in import layout** (from files within \`Research/B01-10/P17Ghost/\`, only after B01-40/B01-60 review, with Codex/B01-20 integration authorization):

\`\`\`
Assets/
  B01TrainingGhost.meta          <- source-owned B01TrainingGhost.meta
  B01TrainingGhost/
    GhostCore.cs                  <- source-owned GhostCore.cs
    GhostCore.cs.meta             <- source-owned GhostCore.cs.meta
    GhostVisualProxy.cs          <- source-owned GhostVisualProxy.cs
    GhostVisualProxy.cs.meta     <- source-owned GhostVisualProxy.cs.meta
\`\`\`

Fixed original SHA-256-derived 128-bit Unity GUIDs:
- Folder \`B01TrainingGhost.meta\`: \`8d7f2b090c0d4ef3ab2436c855d0b104\`.
- Core \`GhostCore.cs.meta\`: \`74f1111e2e76821101d5ee4b43683a33\`.
- Visual \`GhostVisualProxy.cs.meta\`: \`8e613294fbd6ad5dff7a0b23a5aa96e0\`.

**Installer / receiver gate**: Before install, compare all three GUIDs against *the current authorized Codex Unity project* and refuse any existing destination path, GUID collision or already-present CHL ghost owner; the producer's generic CI source inspection does not prove these GUIDs are collision-free in the live private worktree. Copy only the two real C# files and their exact adjacent .meta plus the source-defined folder .meta; do not move the test runner or MANIFEST into \`Assets\`, which may cause unwanted .NET/Unity compilation. Create a fresh empty root \`GameObject\` named \`TrainingGhost_VisualOnly\`, add **only \`GhostVisualProxy\`** (no collider or Rigidbody), call \`Bind\` on an authoritative completed \`GhostRun\`, feed \`Seek\` with the opt-in playback clock, and \`Hide\` during transition. Do not wire replay to CHL-003's actual actor slot, gate triggers or win logic. On rollback, remove only this optional root, new 2 scripts/3 meta; restore any authorized scene/prefab byte for byte.

**Actual source tests vs unexecuted native tests**: \`Program.cs\` increases to **62 authored original C# tests** (including 8 new bound-playback/anti-tamper/high-frequency seek tests and 100 generated synthetic sample traces in original test #50). The portable .NET8 workflow is designed for Windows+Ubuntu, but **only a completed workflow run can establish native execution of those C# cases**. The Unity \`GhostVisualProxy.cs\` is preprocessor-isolated from .NET8, so real Unity Editor compilation, shader behavior/transparency, mesh appearance, physics zero-collider inspection, three retries, human visual readability, and true Light/Heavy physics remain **NATIVE_UNITY_NOT_RUN** until a licensed Unity6000.3.14f1 worktree performs independent PlayMode QA.

**QA request:** B01-40 source G0 check own-path/manifest/source/hash and 3 GUIDs; B01-60 independently execute 62 original portable C# tests on current SHA and inspect negative pose/time/clone paths; B01-20 only approves opt-in CHL host lifecycle; B01-00 controls Codex dispatch and ACK. \`G0=PENDING/G1=NOT_RUN/CODEX_ACK=PENDING\`.


## PASS17 WAVE08 — actual RED → GREEN runner fix, reproducible physical Unity ZIP

The original W06/W07 dedicated workflow contained **literal backslashes** before GitHub expression interpolations, notably \`runs-on: ${{ matrix.os }}\`, not a valid evaluated runner selector. Both jobs remained QUEUED despite ordinary CI-01 running normally. W08 **repaired all six escaped expression occurrences**, including runner and upload-artifact destinations: valid workflow has \`runs-on: ${{ matrix.os }}\` with NO literal leading backslash. GitHub jobs began running after the patch. No change to the game's canonical CI-01/02/03 workflow; no Unity source imported into shared Assets.

**Real physical source package tool** [\`pack_ghost.py\`](pack_ghost.py) is Python3.8+ stdlib-only and produces a deterministic \`B01-10_GHOST001_R2_UNITY_SOURCE.zip\`. It fails closed unless the exact existing eight source/meta/docs files' **raw Git blob IDs** agree with this version's MANIFEST.json, source-only gates explicitly show native Unity NOT_RUN and CODEX ACK PENDING, all 3 GUIDs are correct and unique, the original core has no Unity dependency, and the Unity-only visual is preprocessor-guarded. It neither changes files nor accesses game/Codex code.

The physical ZIP contains **exactly ten real members**, not a shell/reference-only receipt:

\`\`\`
UnityInstall/Assets/B01TrainingGhost.meta
UnityInstall/Assets/B01TrainingGhost/GhostCore.cs
UnityInstall/Assets/B01TrainingGhost/GhostCore.cs.meta
UnityInstall/Assets/B01TrainingGhost/GhostVisualProxy.cs
UnityInstall/Assets/B01TrainingGhost/GhostVisualProxy.cs.meta
Evidence/Ghost.Runner.csproj
Evidence/Program.cs
Evidence/README.md
Evidence/MANIFEST.json
SOURCE_RECEIPT.json
\`\`\`

Member timestamps fixed to 1980-01-01, permissions 0644 and storage method ZIP_STORED. Each member has byte count and SHA256 in \`SOURCE_RECEIPT.json\`, plus the original immutable PR HEAD and **explicit** \`native_unity: NOT_RUN\`, \`independent_G0: PENDING\`, \`codex_ack: PENDING\`. The verifier re-opens original actual ZIP bytes and rejects all member SHA/bad-member/forged-ACK mismatches. The negative runner rebuilds and rejects six real mutated ZIPs: source tamper, .meta GUID drift, fabricated Codex ACK, fabricated Unity test PASS, injected path traversal into Codex R1, and missing Unity visual source.

Commands run from repository root, replace the ref with exact current 40-hex commit:

\`\`\`bash
python Research/B01-10/P17Ghost/pack_ghost.py pack --sha <EXACT_GIT_HEAD> --zip /tmp/B01-10_GHOST001_R2_UNITY_SOURCE.zip --report /tmp/ghost-zip-build.json
python Research/B01-10/P17Ghost/pack_ghost.py verify --sha <EXACT_GIT_HEAD> --zip /tmp/B01-10_GHOST001_R2_UNITY_SOURCE.zip --report /tmp/ghost-zip-verify.json
python Research/B01-10/P17Ghost/pack_ghost.py negatives --sha <EXACT_GIT_HEAD> --zip /tmp/unused.zip --report /tmp/ghost-zip-negatives.json
\`\`\`

The dedicated [GitHub Actions workflow](../../../../.github/workflows/b01-10-p17-ghost.yml) now automatically executes **real original compiled .NET8 C# 62 named tests, 100 seeded synthetic traces, --demo JSON, invalid CLI exit2, this source ZIP build/readback and all six negative ZIP mutants** on both Windows 2025 and Ubuntu 24.04, uploading original ZIP and logs. Native Unity tests still require a licensed disposable 6000.3.14f1 installation, checking actual shaders, serialized GUID collisions, render and zero Collider. Synthetic test runs are *not* native physics telemetry.

**Receiver action after actual final-head CI:** B01-40 verifies ORIGINAL physical ZIP checksum and member bytes on both OS, all 3 .meta GUID vs current authorized Codex worktree, source-tree/manifest and installation+rollback; B01-60 independently executes compiled original C#, mutated ZIP verification and actual Unity native tests only with real Editor. B01-20 confirms the optional real CHL event interface. B01-00 alone can dispatch via #79 after explicit independent approval; do not treat SOURCE_ZIP available, CI-green or this producer handoff as \`OFFERED/FROZEN\`, \`CODEX_CLAIMED\`, \`UNITY_G1_PASS\` or \`INTEGRATED\`.


## PASS17 WAVE09 — GHOST-001/R3: real-time coaching and opt-in HUD

**One new functional part on existing package #422, no third PR and no other-owner change.** The previous W08/R2 original head \`e2af0bc5e23958c24846ae4015fdf5e18b2c3004\` with its 62/62 C# tests and ten-member physical ZIP is a *historical verified version*; its earlier checks **do not automatically prove W09's new source**. The R3 revision is separately pinned in this HEAD's MANIFEST and revalidated by CI.

**Original actual gameplay-support C#:** [\`GhostSplitCoach.cs\`](GhostSplitCoach.cs) consumes only a host-validated checkpoint index and *authoritative CHL run elapsed time*. It compares each timed *segment* and cumulative gate elapsed with a completed benchmark run, classifies the segment as \`Ahead/Even/Behind\` at ±0.15s (configurable only by code), and reports the greatest measured loss segment after a genuine completed attempt. It does not infer vehicle physics faults from time, invent gates or apply gameplay forces/rewards/score. Failed runs never become valid personal records. Wrong-order/duplicate/skipped checkpoints, NaN or non-increasing timestamps, double finish and false early completion are rejected before any state mutation. \`Retry()\` resets only local coaching progress while preserving a validated defensive copy of the benchmark.

**Actual optional Unity 6 URP UI source:** [\`GhostSplitHud.cs\`](GhostSplitHud.cs) is a small self-contained \`MonoBehaviour\` using built-in \`OnGUI\`. On a host-authorized standalone GameObject, \`BindCompletedReference(completedGhostRun)\`, \`OnHostValidatedGate(zeroBasedIndex, realElapsed)\`, \`OnHostValidatedEnd(realSuccess, realElapsed)\`, and \`Retry()\` display measured per-gate pace and largest loss segment. It creates no collider, Rigidbody, checkpoint trigger, rewards, save, player inputs or game-state side effects. The actual CHL course and four physical gate callbacks remain exclusively under B01-20. \`GhostSplitHud\` alone cannot verify that a vehicle really crossed a gate; host passes through only *already-validated* callbacks.

**New fixed Unity GUIDs**:
- \`GhostSplitCoach.cs.meta\` = \`7cf6f79b815d4778ab69d561811a6001\`;
- \`GhostSplitHud.cs.meta\` = \`407311dc82d84a41baf4fe8f1ae028a6\`.

**Actual receiver import extension** relative to W08: the same \`Assets/B01TrainingGhost/\` opt-in installation now includes **four** original \`.cs\` files and their **four** matching \`.meta\` GUIDs, plus the one folder \`Assets/B01TrainingGhost.meta\` (five GUIDs total). New files \`GhostSplitCoach.cs[.meta]\` + \`GhostSplitHud.cs[.meta]\` must pass the same independent GUID-collision gate. **No edits to canonical Assets here; only source under Research/B01-10.** Rollback deletes only that new isolated source folder and explicitly authorized standalone HUD/visual GameObject. Never touch real B01-20 CHL gate triggers or Codex integration source.

**Deterministic physical ZIP R3 now contains fourteen actual members**: nine \`UnityInstall/Assets/...\` entries (four C# sources, four script .meta and one folder .meta), four \`Evidence/...\` entries (Program.cs, csproj, README, manifest), and \`SOURCE_RECEIPT.json\`. The producer-original \`pack_ghost.py\` verifies exact current manifest raw Git blobs, all five unique GUIDs, deterministic raw ZIP, original current-head Git SHA, no fake independent QA/Unity/ACK, and rejects six actual mutated ZIPs. Its test commands still work with \`--sha CURRENT_HEAD\`; use the *new* final-head ZIP checksum, **never reuse old W08 ten-member archive SHA**.

**Tests:** new \`Program.cs\` cases #63–#82 are 20 additional *original compiled C#* positive/adverse checks for live segment loss, ahead/even/behind, cumulative vs segment differences, biggest-loss guidance, malicious earlier/duplicate/missing gates, nonfinite time, rejected false completion, failed attempts, safe retry and benchmark/result defensive copying. The complete suite is **82 authored named test cases plus 100 deterministic synthetic traces**; only mark them \`PASS\` after observing a new exact-head original Windows/Ubuntu GitHub runner, with real source/ZIP SHA. **Native Unity Editor compilation, overlay display and real physical Light/Heavy playthrough remain NOT_RUN until a licensed Unity 6000.3.14f1 receiver performs them**, regardless of .NET CI.

**B01-40/B01-60 formal G0/QA requested; Codex busy, no ACK, no install, no merge.**


## PASS17 WAVE10 — GHOST-001/R4: actual final-leg coaching error corrected

**Concrete source bug and observed misleading guidance in the R3 algorithm**: the R3 \`GhostSplitCoach.EndTrial\` measured \`BiggestLossGate\` solely by enumerating the N checkpoint-to-checkpoint segments. A real player could pass all four CHL checkpoints at the benchmark times, then take **6 seconds instead of 1 second** from checkpoint #4 to actual finish. The source evaluator then incorrectly showed *no measured time loss*, despite finishing **+5s slower**. The largest time-loss analysis **omitted the last leg entirely**. This is fixed in existing source \`GhostSplitCoach.cs\`; no change to CHL triggers/authoritative race logic.

**R4 behavior**:
- \`EndTrial(completed=true, actualRealElapsed)\` now measures an additional finish segment: \`actualRealElapsed - lastAcceptedGateElapsed\` vs \`reference.ElapsedSeconds - reference.CheckpointSeconds[N-1]\`. Compares that segment with the previous N ones for the greatest positive loss.
- The result adds \`FinishSegmentSeconds\`, \`ReferenceFinishSegmentSeconds\`, \`FinishSegmentDeltaSeconds\`, and \`BiggestLossIsFinishSegment\`. For a worst final leg, the existing \`BiggestLossGate\` is **0** with the new bool set \`true\`, so no fake "checkpoint #5". Actual advice explicitly says *finish segment after checkpoint N*.
- A failed/aborted run has NO verified finish, so the final-leg fields remain zero and it never invents a finish-time diagnosis. Rejected invalid completed attempts do not commit \`finished\`, allowing later legitimate gate callbacks and finish.
- The coach now rejects a *reference* with any zero-duration gate-to-gate or final checkpoint-to-finish segment upon \`new GhostSplitCoach(reference)\`. Such deserialized/edge-case tracks may be valid archive records under the general \`GhostMath.Validate\` contract but cannot support measurable training advice. This check is local to the coach; original generic recording/serialization contract remains unchanged.
- Optional Unity \`GhostSplitHud\` distinguishes "finish after gate N" from "gate N", and displays retry rather than fake "no losses" for aborted attempts. It still creates no physics actors, colliders, rewards, or CHL gates; no canonical \`Assets/**\` edited.

**14 additional genuinely executable regression cases #83–96** in original \`Program.cs\`: finish loss outranks all gate splits, exact and faster finishes, gate loss outranks finish loss, false early finish rejected/recoverable, failed-run zero finish statistics, zero-length final/gate/first reference legs rejected, measured text, defensive reference clone, tolerance and deterministic tie resolution. Complete **96 original C# test definitions**, plus 100 seeded *synthetic* trajectories and six adversarial mutated physical archives. Real \`dotnet\`/ZIP PASS must be read from the final SHA's Windows+Ubuntu Action logs, NOT inferred from the test sources or historic W09 82/82.

**Physical Unity source layout unchanged**: 4 C# portable/Unity source files + their 4 \`.meta\`, folder \`.meta\`, 4 evidence files and one receipt = **14 exact ZIP members**. The original cross-platform deterministic packer verifies actual raw source Git blobs from this R4 manifest and generates the new SHA256; do not reuse W09 archive's older checksum \`2f5d754c411c4a537a68db582630b7c6d9299d9544da681ce0e29f8039be7e74\` as an R4 receipt. Rollback is unchanged: only new isolated \`Assets/B01TrainingGhost\` and opt-in HUD/ghost root after authorized B01-40/60/20 native acceptance. Native Unity Editor/PlayMode/actual CHL physics still **NOT_RUN**, G0 and Codex ACK **PENDING**.


## PASS17 WAVE11 — GHOST-001/R5: abort-safe full-course timing (B01-10)

**Real source defect fixed:** in R4, a failed/aborted trial ending after 0.5 seconds with a 5-second completed reference was returned as `TotalDeltaSeconds=-4.5` seconds: a mathematically accurate subtraction but an invalid full-course performance comparison. This could incorrectly appear as a faster finish to any consumer of `GhostPracticeSummary`.

**R5 behavior:** `GhostPracticeSummary.HasFullCourseComparison` is true ONLY after host-authorized completed four-gate finish, and `TotalDeltaSeconds` is then signed actual finish minus reference finish. For incomplete attempts, `HasFullCourseComparison=false` and `TotalDeltaSeconds=0` (a deliberately invalid sentinel; never present as a speed tie). `TrialSeconds` and `ReferenceSeconds` retain their individually meaningful elapsed values, and already-measured gate split losses remain diagnostic; a failed trial never pretends it won or reached the finish. The opt-in Unity HUD explicitly says `No full-course comparison (attempt incomplete)` and shows a signed full-course delta only for completed runs. This is a read-only training indicator, NOT CHL finish logic, rewards or a gameplay clock.

Four new original C# regression tests #97–100 exercise early abort, partial gates plus loss, exact completed reference, and five-second late completed finish. Current count **100 authored original tests**, plus the previous 100 seeded **synthetic** traces and six negative ZIP mutations. Only a new exact-SHA executed CI run establishes PASS; historical R4 96/96 on Windows/Ubuntu is not proof for R5. The original five Unity GUIDs and 14-member deterministic importer are unchanged. Native licensed Unity HUD/render/PlayMode and true Light/Heavy physics remain NOT_RUN; independent B01-40/B01-60 G0/QA and B01-20 host contract remain pending. R4 exact commit `3ad724b45a809cd674043bdb4c086f0ac6dfbb24` remains immutable in history; no external game/Codex paths or frozen sources touched.

## PASS17 / WAVE12 — GHOST-001/R6: actionable pace focus, no subthreshold false cues

**Original live HUD defect:** R5 `GhostSplitCoach.EndTrial` correctly records the largest positive gate loss even when it is under `PaceToleranceSeconds=0.15`, and its `Guidance` correctly reports no meaningful difference. The Unity HUD ignored that threshold and displayed `Focus: gate 1` for a 0.1s loss. It could contradict the coach guidance and waste human playtest effort. This is a **real original code branch discrepancy** found by inspection; native Unity reproduction remains NOT_RUN.

**R6 fix:** new pure, game-portable `GhostPracticeFocus.Text(summary, requiredGates)` is the **single deterministic focus presentation policy** shared by the Unity OnGUI overlay and original .NET8 C# runner: incomplete trials always get retry, completed trials within inclusive 0.15s tolerance get neutral, significant finish losses get final-leg focus, significant gate losses get the relevant real gate; invalid missing gate/NaN focus data fail closed. `BiggestLossGate` is preserved as measured diagnostic value even under tolerance, never rebranded as actionable focus. No changes to physics, CHL callbacks, art, rewards, scoring, RNG, canonical Codex state or public host methods. Existing five Unity GUIDs unchanged. Source file scope only existing B01-10 package paths and own workflow.

**Positive and adverse portable tests:** original new cases #101–109 verify real actual source coach-driven 0.1s case, significant gate loss, significant final leg, abort, tie, 0.15s inclusive tolerance, null summary, invalid gate count, missing gate for significant loss. Total 109 compiled original C# named assertions + 100 explicitly SYNTHETIC traces + original six archive-corruption negatives. Must report new SOURCE_SHA and actual executed CI before claiming a R6 PASS. Source-level verification does NOT prove Unity HUD rendering; authorized Unity 6000.3.14f1 native Editor/PlayMode and B01-60 independent QA remain NOT_RUN/PENDING. Installer, 14-member ZIP and rollback remain as in R5, with version updated to R6. Previous R5 immutable ancestor `13a1a4770728f4e9d5704150afbe25483ca88ca9` preserved. B01-40 G0, B01-60 independent QA, B01-20 CHL host acceptance, B01-00 eventual routing; no offer/ACK/merge.

## PASS 17 / WAVE 13 — GHOST-001/R7: targeted visual practice loop

**New real opt-in feature:** after a host-authorized completed CHL practice attempt, `GhostSplitCoach.RecommendedLoop` selects precisely the **largest measured significant time-loss segment**, including checkpoint→finish. It returns an immutable `GhostPracticeLoop` spanning the *validated reference run's* corresponding segment (e.g., gate 2 = [reference gate 1, reference gate 2]; finish = [last gate, completed finish]). It only exists when the trial completed and the loss exceeds `PaceToleranceSeconds=0.15`. Aborted, tied, under-tolerance, not-yet-ended trials yield null — never fabricate a drill. Ties preserve the earliest gate.

**Executable playback:** `GhostPracticeLoop.PoseAt(double loopElapsedSeconds)` computes a deterministic modulo repeat of just the selected reference segment using the existing validated `GhostPlayback` source and shortest-yaw interpolation. Finite nonnegative [0,86400] visual replay seconds only, invalid inputs fail closed, caller pose edits do not mutate the track. The result is a reference visual pose, not an instruction to the player's Rigidbody or a game win checkpoint. Original Unity 6 `GhostVisualProxy.BindDrill(plan)` / `SeekDrill(double)` provides an opt-in visual-only 3D URP arrow looping on a new dedicated empty root; `GhostSplitHud.RecommendedLoop` and HUD label display the segment's real reference time window. **No auto-hook to CHL**, pause/clock/authoritative physics remain owned by B01-20; host must provide an explicit visual clock. Do not install on real vehicle or pass replay pose into gameplay. No new colliders, force, economy, run save, rewards, AI, scoreboard or Codex `Assets/**` writes.

**Positive/negative original portable C# tests:** #110–122 cover first gate, middle gate, finish, exact wrap, meaningful/tie/abort/tolerance, retry, invalid clocks, defensive reference clone, defensive returned pose and true host-finish requirement. Total **122 named source C# tests** plus previous **100 synthetic traces** and **six real corrupted ZIP mutants**, each OS when workflow actually executes. Prior R6 `b2c73f0966aed648de951c4ee528e3c51c7f8e41` remains an immutable recoverable Git commit; R7 test status must be read from its own exact HEAD and actual logs. Physical importer remains 14 files, all existing five Unity .meta GUIDs stable. B01-40 independent G0, B01-60 independent QA and licensed Unity 6000.3.14f1 G1, B01-20 CHL host validation, B01-00 future authorized routing; G2 HOLD, CodeX ACK NO, NO MERGE. Rollback remove ONLY own optional new `Assets/B01TrainingGhost` source/visual roots after authorized scratch install; never touch shared CHL/course/canonical scenes. R7 source ZIP script remains hardened against changed bytes, forged ACK/Unity, ZIP injection and wrong GUID.

## PASS 17 / WAVE 14 — GHOST-001/R8: internally consistent terminal outcomes

**Concrete RED found by inspecting original R7:** `GhostMath.Validate` only checked the `Completed` boolean and checkpoint coverage, not the independently stored `EndReason`. A failed run can legitimately have **all four checkpoint timestamps** (it may crash/fail after gate #4 but before finishing); flipping just its `Completed` flag to `true` would pass R7 validation while `EndReason="Failed"`, allowing `GhostBestBook.Submit`, `GhostPlayback`, and `GhostSplitCoach` to accept a contradictory run as a completed benchmark. This is not proof of malicious behavior; deserialization, erroneous UI event wiring, or stale field mutation can cause this mismatch.

**Original R8 source fix:** `GhostMath.Validate` rejects all incompatible terminal outcomes: `Completed=true` requires `EndReason="Completed"`, `Completed=false` requires `EndReason="Failed"` (ordinal/case-sensitive), and null/blank/alternate reasons fail closed. The original trusted `GhostRecorder.Finish` and `Fail` already produce those exact strings; no protocol, clock, running physics, Unity GUID or public host signature is changed. The check is applied by the shared original `Validate` path used by playback, personal-best storage, comparison, cloning and training coach. This integrity guard is **not a cryptographic authenticity guarantee**: a fully forged, internally consistent recording still requires a trustworthy physical CHL host to reject it. It is unrelated to detached-part ID persistence or B01-23/62 Save/Load #510.

**Original executable regression cases:** #123–134 test failed-after-all-gates promotion via one-field mutation, mismatched bool/reason in both directions, empty/null/noncanonical terminal reasons, genuine completed and failed compatibility, PB immutability on rejection, replay/coach refusal, cross-rig comparison, and failed-after-all-gates reference refusal. Total **134 original portable named C# cases** + 100 seeded explicitly synthetic traces + original six physically mutated ZIPs per OS, **only upon observed exact-SHA CI PASS**. Existing 14-member import ZIP, 4 Unity C# source files, 5 original GUIDs and strict rollback unchanged. Prior R7 `5ee6f3e61156baf14186f7920685e2536a44812e` preserved as an immutable recoverable ancestor. Independent source G0/QA, actual Unity rendering/Edit/Play, physical host compatibility still PENDING/NOT_RUN; CodeX ACK/merge/integration unchanged HOLD.
