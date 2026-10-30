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
