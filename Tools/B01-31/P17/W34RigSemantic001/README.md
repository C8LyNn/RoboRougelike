# B01-31 · PASS 17 / W34 — execute original R41 v5 rig source semantics, not another prefab

**Original source parent:** [B01-31 #922](https://github.com/C8LyNn/RoboRougelike/pull/922) at immutable `b61bbe0643a6547ccb3087ada44474e8b33550fd`. Ancestors: [#914](https://github.com/C8LyNn/RoboRougelike/pull/914) → [#904](https://github.com/C8LyNn/RoboRougelike/pull/904) → [#893](https://github.com/C8LyNn/RoboRougelike/pull/893). Exactly **five ADD-only** W34 Tools/CI/README paths; **zero modifications** to original Editor, Assets, model, game, Codex #873/#877, Save/ECON, Unity tests, FBX or prior branch.

## Actual improvement in QA confidence

The existing W33 original source checker was principally **textual**: it verified that source code contained fail-closed clauses. This W34 isolated QA adds real **C#9 compilation and execution** of the exact two original production methods (unaltered extraction from the SHA-pinned `Assets/B01_31/W30R41V5Adapter/Editor/B0131W30CreateR41V5AdapterCandidate.cs`):
- `private static Transform FindUnique(...)`;
- `public static bool TryValidateSourceRigHierarchy(...)`.

A minimal, openly declared managed-only `GameObject / Transform / Renderer` *ancestry stub* supplies inputs, solely for source-level control-flow tests; it is **NOT Unity**, real FBX, ModelImporter, PlayMode, PhysX, or a visual rig simulation. The production methods are inserted **verbatim** into a generated temporary test assembly. No source model/game implementation is duplicated or modified.

**14 positive/negative C# behavioral checks:** valid fixed/yaw/pitch Renderer branches; source null; missing/duplicate Yaw/Pitch; Pitch detached from Yaw; left/right muzzle detached from Pitch; missing static/yaw/pitch Renderers; duplicate Muzzle_L; root mistaken for Yaw. Each control calls the real original method and checks exact rejection reason.

**3 negative production-method mutations:** remove fixed-renderer admission, bypass pitch/yaw ancestry, bypass both muzzle ancestry checks; each must cause the C# test process to fail with `SOURCE_SEMANTIC_FAIL`. Original method restored and recompiled at end, requiring all 14 controls to pass again. This tests the *real method's logical contract* more strongly than merely searching for code strings.

## Execution / evidence

GitHub two-OS workflow uses .NET 8 SDK and Python 3.11. Exact W33 file SHA256 `f3429796b0ea8c107cf60590b4a5c7721b98e5d5f02592d4aa6e9957320239a3` and the W33 PR HEAD are pinned. Verifier requires exactly 5 W34 additions; it writes original log result and JSON receipts. No Unity license is requested or bypassed.

**Acceptance remains blocked:** the original producer FBX SHA256 `fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533` is outside GitHub. Even successful Win/Ubuntu `dotnet` source execution **does not** prove correctness of `CreateCandidatePreview` (not tested in this harness), actual imported transform poses, dual muzzle fire, collider/renderer display, Player performance or Windows x64 build. B01-30/40 exact independent G0 for #922 is still needed, then B01-60 authentic Unity 6000.3.14f1 W30/W31/W33/#909 native model tests + actual R3 shots/Player. Preserve original old AutomaticGun until accepted.

**Rollback:** omit exactly five new W34 QA paths in approved scratch checkout. Do not revert/delete other agents' files or modify #922 producer, #877 game, original FBX, Save, player account or protected branches.

Status before own W34 CI: `IMPLEMENTED=SOURCE_SEMANTIC_QA | TESTED=NOT_RUN_PENDING_CI | UNITY_G1=NOT_RUN | MODEL=NOT_APPROVED | INTEGRATED=NO | RELEASED=NO`.
