# B01-24 PASS 12 — targeted enemy-AI original Unity NUnit negative gate

**Owner:** B01-24 (QA-only), **production FIX_OWNER:** Codex, **native independent QA:** B01-60.  
**Reviewed canonical source:** [Codex PR #310](https://github.com/C8LyNn/RoboRougelike/pull/310), `231005c6d0351945a8ee6684f3856af118f0e94f`, original enemy turret Git blob `8b0cfacbba967ca1a3e3e514e2cd1b49b805946e`.
**QA branch:** `qa/b01-24-w62-enemy-target-los-negatives`, branched from exact Codex HEAD; only QA NUnit file and this note added, **no edits to Codex's feature branch, no runtime production source changes**.

## Tests in Assets/Tests/Editor/B0124EnemyAiNegativeEditModeTests.cs

These tests invoke **original production C# objects/methods**, not mirrored JS/Python algorithms:

1. `ActiveLivingEnabledBlock_IsSelectable_PositiveControl` — healthy active physical block can be targeted.
2. `DisabledCollider_IsNeverSelectable_NegativeControl` — physical block disabled at Collider level is excluded.
3. `InactiveLivingEnabledBlock_IsNeverSelected_Regression` — when only inactive player child exists, `SelectTargetCollider` must return null, despite a live component and `Collider.enabled=true`. **Expected to fail on reviewed unpatched source**, because `GetComponentsInChildren(...,true)` includes inactive and the selector omits active-in-hierarchy.
4. `DeactivateAndReactivateBlock_ReacquiresOnlyWhenActive` — active → inactive → active target eligibility. **Expected to fail at middle step on unpatched source.** NOTE this tests reevaluation on each direct call, not the distinct `currentTarget` 0.8s cache path.
5. `SolidOpaqueCover_SuppressesShot_ThenRecovers_ConditionalPolicy` — **NUnit [Explicit]**: opt-in after HQ/B01-10 approves *suppressed* fire against invulnerable cover; calls original enemy `Update` and counts actual spawned `P17PhysicalShell` objects with a real `Physics.Raycast` barrier validation. No launch on current HQ-undecided policy. If intentional cover-suppression is approved instead, the test must be re-specified before use; not a generic AI fixing constraint.

## Authentic Unity execution (UNPERFORMED)

Place this isolated QA branch in an approved disposable Unity 6000.3.14f1 worktree with valid license, then run the Editor NUnit suite with the **original Codex production files** present. Example command for a licensed operator (adjust Unity.exe/project path):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\QA\RoboRougelike-B0124" -runTests -testPlatform editmode -testFilter B0124EnemyAiNegativeEditModeTests -testResults "C:\QA\Evidence\B0124-Edit.xml" -logFile "C:\QA\Evidence\B0124-Editor.log" -quit
```

The first four tests should run without product policy. NUnit may report **2 expected RED** before the fix, but **none have actually run in Unity in this swarm environment**, so do not assert any real 2/4 result until the XML/Editor.log/exit receipt is collected. The fifth is [Explicit] and requires separate product-policy authorization. B01-60 should execute independently; Codex should repair target selection and cache invalidation only after reproducing actual failure.

## What this does NOT prove

- No real Unity import, compilation, original NUnit execution, PhysX or Windows Player test was performed in this authoring session.
- No original gameplay LOS policy has been decided by HQ. The conditional cover case cannot be used as an unconditional merge requirement.
- The test does **not** assert the full vehicle's own active-core lifecycle permits an inactive still-undead physical player part. If no supported live-game transition can produce that state, Codex/B01-60 should classify the issue `UNREACHABLE` and avoid unnecessary changes.
- G0 CHL is an independent owner gate: [CHL #285](https://github.com/C8LyNn/RoboRougelike/pull/285) progressed to `c7b2a234a286089383503da41883d624fe650551`; this QA branch is NOT a CHL producer/receiver artifact and must not be counted as CHL G0/G1.
