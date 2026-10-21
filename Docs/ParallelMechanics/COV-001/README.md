# COV-001 — Destructible Arena Cover (R1)

**Issue:** https://github.com/C8LyNn/RoboRougelike/issues/11  
**Target:** Unity 6000.3.14f1, branch `feature/rrl-cov001-isolated-pack`, DRAFT PR → `develop`.

## Behavior

- Arena-only breakable obstacle, independent from vehicle part durability and weapon/damage pipeline.
- Configurable HP (default 50). `ApplyDamage(amount)` accepts only finite positive values, clamps overkill and ignores further hits after break.
- Four stages: Intact, Damaged (remaining HP ≤ 66%), Critical (≤ 33%), Broken (0 HP). Optional per-stage renderer groups (no material edits).
- On break, disables only captured non-trigger colliders belonging to this cover. Explicit blocker list is available. `ResetCover()` restores **initial** enabled/disabled states and full HP.
- `DamageAccepted`, `StageChanged`, `BrokenOnce` C# events; serialized UnityEvents `onDamaged` and `onBroken`. A single break emits `BrokenOnce` only once; reset starts a new cycle.
- Does not register global damage hooks, does not mutate vehicle parts, and does not modify existing scene/prefab assets.

## Opt-in demonstration

In Unity Editor choose **Tools → RoboRougelike → COV-001 → Create Demo Prefab**. This creates `Demo/COV001_ArenaCover.prefab` *only if absent*; it is not auto-added to the game scene. Drag the generated prefab into a **separate temporary test scene**. In Play Mode use the `COV001DemoDriver` component context menu **Apply Demo Hit** (20 HP) or **Reset Cover**. The generator does not overwrite an existing prefab.

For real gameplay integration, a future adapter should subscribe to a validated projectile-hit event and call `ApplyDamage` on the hit cover. This is **NOT IMPLEMENTED** and must not be simulated by scanning scene objects. Do not touch R1/R3/R4/R6, weapons, AI, MOV, rewards or the main scene without new authorization.

## Tests

- EditMode: 5 NUnit cases (initial state, invalid values, stage transitions, break/reset idempotence, overkill event).
- PlayMode: 2 UnityTest cases (collider lifecycle, repeated hits).
- Run on a separate Unity 6000.3.14f1 checkout with a valid license: `Tools/CI/Run-Unity.ps1 -Task EditMode` and `-Task PlayMode`.
- **Status: NOT RUN** for both suites in this implementation pass; GitHub/structural validation cannot substitute for Unity tests. No Windows build was run.
- Before merging: execute both suites, manually exercise the generated prefab, check console and collider behavior, and validate no regressions in the Gauntlet scene.

## Boundaries

All new/changed files reside in `Assets/RoboRougelikeMechanics/COV001DestructibleCover/**` or `Docs/ParallelMechanics/COV-001/**`; the original parent-folder `.meta` is untouched. No main/develop changes or automatic merge.
