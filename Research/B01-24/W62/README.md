# B01-24 PASS 12 — targeted enemy-AI original Unity NUnit negative gate

**Owner:** B01-24 (QA-only), **production FIX_OWNER:** Codex, **native independent QA:** B01-60.  
**Reviewed canonical source:** [Codex PR #310](https://github.com/C8LyNn/RoboRougelike/pull/310), `231005c6d0351945a8ee6684f3856af118f0e94f`, original enemy turret Git blob `8b0cfacbba967ca1a3e3e514e2cd1b49b805946e`.
**QA branch:** `qa/b01-24-w62-enemy-target-los-negatives`, branched from exact Codex HEAD; QA-only NUnit sources, stable Unity .meta files and this note added, **no edits to Codex's feature branch, no runtime production source changes**.

## Tests in Assets/Tests/Editor/B0124EnemyAiNegativeEditModeTests.cs

These tests invoke **original production C# objects/methods**, not mirrored JS/Python algorithms:

1. `ActiveLivingEnabledBlock_IsSelectable_PositiveControl` — healthy active physical block can be targeted.
2. `DisabledCollider_IsNeverSelectable_NegativeControl` — physical block disabled at Collider level is excluded.
3. `InactiveLivingEnabledBlock_IsNeverSelected_Regression` — when only inactive player child exists, `SelectTargetCollider` must return null, despite a live component and `Collider.enabled=true`. **Expected to fail on reviewed unpatched source**, because `GetComponentsInChildren(...,true)` includes inactive and the selector omits active-in-hierarchy.
4. `DeactivateAndReactivateBlock_ReacquiresOnlyWhenActive` — active → inactive → active target eligibility. **Expected to fail at middle step on unpatched source.** NOTE this tests reevaluation on each direct call, not the distinct `currentTarget` 0.8s cache path.
5. `InactiveCachedTarget_IsInvalidatedAtNextAiUpdate_Regression` — **original** `Update()` is invoked while `currentTarget` points to an inactive collider and `nextTargetTime` is in the future; requires immediate eviction. **Expected RED on unpatched #310**; shot emission is suppressed with an intentionally zero max range for isolation.
6. `SolidOpaqueCover_SuppressesShot_ThenRecovers_ConditionalPolicy` — **NUnit [Explicit]**: opt-in after HQ/B01-10 approves *suppressed* fire against invulnerable cover; calls original enemy `Update` and counts actual spawned `P17PhysicalShell` objects with a real `Physics.Raycast` barrier validation. No launch on current HQ-undecided policy. If intentional cover-suppression is approved instead, the test must be re-specified before use; not a generic AI fixing constraint.

## Authentic Unity execution (UNPERFORMED)

Place this isolated QA branch in an approved disposable Unity 6000.3.14f1 worktree with valid license, then run the Editor NUnit suite with the **original Codex production files** present. Example command for a licensed operator (adjust Unity.exe/project path):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\QA\RoboRougelike-B0124" -runTests -testPlatform editmode -testFilter B0124EnemyAiNegativeEditModeTests -testResults "C:\QA\Evidence\B0124-Edit.xml" -logFile "C:\QA\Evidence\B0124-Editor.log" -quit
```

The first five tests should run without product policy. NUnit may report **3 expected RED** before the fix, but **none have actually run in Unity in this swarm environment**, so do not assert any real NUnit result until the XML/Editor.log/exit receipt is collected. The sixth is [Explicit] and requires separate product-policy authorization. B01-60 should execute independently; Codex should repair target selection and cache invalidation only after reproducing actual failure.

## What this does NOT prove

- No real Unity import, compilation, original NUnit execution, PhysX or Windows Player test was performed in this authoring session.
- No original gameplay LOS policy has been decided by HQ. The conditional cover case cannot be used as an unconditional merge requirement.
- The tests do **not** assert the full vehicle's own active-core lifecycle permits an inactive still-undead physical player part. If no supported live-game transition can produce that state, Codex/B01-60 should classify the issue `UNREACHABLE` and avoid unnecessary changes.
- G0 CHL is an independent owner gate: [CHL #285](https://github.com/C8LyNn/RoboRougelike/pull/285) progressed to `c7b2a234a286089383503da41883d624fe650551`; this QA branch is NOT a CHL producer/receiver artifact and must not be counted as CHL G0/G1.

## PASS13 extension — exact original LD02 physical map fixture

**NEW** [`B0124LD02PhysicalLosPlayModeTests.cs`](../../../Assets/Tests/Editor/B0124LD02PhysicalLosPlayModeTests.cs), authored under the SAME B01-24 QA branch. A stable, tracked `.meta` was also added for this source and for the older enemy fixture, to avoid untracked Unity import-generated metadata and changing test script identity.

Original **B01-25** [LD02.json](https://github.com/C8LyNn/RoboRougelike/blob/bdd6c69fc9fd519b863e3a9cb217b3cd39404194/Research/B01-25/P11/LD02.json) blob `71c305077b97015a3a4dad7a5fc64dea59943617`: 10 existing map items, unique Spawn/Finish, four Pillars, two LowBarriers, two Walls, GlobalFloor ON. New test invokes **original production** `MapDraft.Place`, `MapWorld.Build` and Physics.Raycast in a real Unity `EnterPlayMode` transition, using all ten exact source coordinates.

**Expected geometry at actual physics runtime, NOT yet verified:** 
- BLOCKED static ray: origin `(12,1.5,0)` → `(12,1.5,16)`, intersects original Pillar at cell `(3,2)` / world `(12,2,8)`; y=1.5 passes through original collider's height 1..3.
- CLEAR static control: `(16,1.5,0)` → `(16,1.5,16)`, offset 4 m from same Pillar.
- RECOVERED: disable *only* actual `MapItemReference` Pillar (3,2), `Physics.SyncTransforms`, ray previously blocked must be clear.

**Important:** these are test-only static **world ray anchor** points at y=1.5; a true enemy's muzzle can be at another height/position and must be separately measured. This test verifies source map cover collider provenance, **not enemy gun damage, actual AI cover-fire semantics, active-player encounters or LD02 kill-2 completion**. Unity screenshot/trajectory and PlayMode XML, log, exit remain REQUIRED; no gameplay victory or native PASS is claimed.

**Operator run** on approved licensed Unity 6000.3.14f1 disposable host, with actual original Codex source unchanged:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\QA\RoboRougelike-B0124" -runTests -testPlatform editmode -testFilter B0124LD02PhysicalLosPlayModeTests -testResults "C:\QA\Evidence\B0124-LD02.xml" -logFile "C:\QA\Evidence\B0124-LD02-Editor.log" -quit
```

Test class is in the **Editor** assembly and transitions to true Play Mode via `EnterPlayMode`; do not reclassify it as having run just because it was authored.

PASS13 independent original-source verification: **13/13** exact LD02 original JSON versus newly authored 10 `MapDraft.Place` calls, negative deleted-item mismatch, original MapWorld API/geometry anchors, both `.meta` identities, and QA-only Git diff gates. These are NOT Unity or original C# execution.

**PASS13 separate finding:** [ENEMY-TARGET-INACTIVE-01 #318](https://github.com/C8LyNn/RoboRougelike/issues/318) is independently routed for Codex with a minimal native test contract. HQ/B01-10 still owns whether opaque *static* cover suppresses all enemy normal rounds; do not enable the `[Explicit]` gate without a product decision.

## PASS14 — corrected original-NUnit diagnostic and CV-06 handoff

**QA-only correction (no game-AI changes)** to `B0124EnemyAiNegativeEditModeTests.SolidOpaqueCover_SuppressesShot_ThenRecovers_ConditionalPolicy`: previously, `Assert.That(blockedShotCount, Is.Zero)` executed **before** the recovered-wall step. On the unpatched source a blocked shot can emit a shell and fail immediately, masking whether clearing LOS restores fire. Additionally, that shot advances `nextFireTime`, so a same-frame second call can appear incorrectly broken merely due to cooldown.

The test now:
1. Fires once with a genuine world `Physics.Raycast`-confirmed static wall; records only new original `P17PhysicalShell` instances as `blockedShotCount`.
2. Disables only that wall, calls `Physics.SyncTransforms()`, restores **only** cooldown eligibility `nextFireTime=-1`, and invokes original `Update()` again.
3. Records `recoveredShotDelta = totalNewShells - blockedShotCount` and writes **both observed values** to NUnit `TestContext.WriteLine` with source SHA.
4. Applies `Assert.Multiple` **after both observations**: require `blockedShotCount == 0` (ONLY after B01-10/HQ approves suppressing opaque direct shots), `recoveredShotDelta == 1` (true recovery).
5. Removes only newly created shells in `finally`.

This repair is **of the QA test's diagnostic fidelity**, not a Codex gameplay fix. The test remains `[Explicit]`, excluded from ordinary automated runs until HQ approves the cover-fire policy. The disabled/inactive target tests are separate source-risk and are **not** gated by this optional policy.

**Queue ownership:** [CODEX CV-06](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223) already includes both target-inactive [#318](https://github.com/C8LyNn/RoboRougelike/issues/318) and clear/covered/recovered LOS. **Do not add a duplicate queue item.** On an actually activated authorized Unity host, Codex can import this QA-only branch separately into an isolated disposable checkout, execute its original EditMode fixtures, and append raw `EditMode.xml`, `Editor.log`, exit status and source SHA. For any native collision/cover test include actual `Physics.Raycast` first hit (instance ID, name, distance), turret muzzle `Transform.position`, source map hash, `blockedShotCount`, `recoveredShotDelta` and explicit B01-10/HQ policy. Do not claim the queue is ACKED or that a licensed runner exists until Codex reports it.

**No native test execution occurred during PASS14 authoring** (Unity and C# compiler unavailable). The previous static checks do not become an executed original NUnit PASS.
