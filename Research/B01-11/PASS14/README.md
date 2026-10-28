# B01-11 / PASS14 — AIM-U01/U02: reticle affordance vs actual target

**Status: `SOURCE_RISK_REPRODUCED / NATIVE_PLAYER_NOT_RUN / CODEX_FIX_PENDING`.**
This is a reproducible, scope-limited UX acceptance input for [Codex owner task #275](https://github.com/C8LyNn/RoboRougelike/issues/275) and existing [CV-07 full-stack/smoke #79](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223). **No extra CV ticket**: #275 already owns camera-ray / barrel aim / terrain marker, CV-07 already requests native 720p/1080p aimed play. This does not replace #275's full long-distance physical projectile tests.

## Original immutable source

Codex #310 integrated source commit **`231005c6d0351945a8ee6684f3856af118f0e94f`**, exact original Git blobs:

| Path | Git blob SHA-1 |
|---|---|
| `Assets/WeaponIntegration/RRLAimFeedback.cs` | `503016450ba51b9112fa47dc27118120b5ba366b` |
| `Assets/R6/R6RangeHUD.cs` | `adfd3c1a69fc4355e055581a736ce10cccc7ba91` |
| `Assets/Pack/P12_PipelineSafeTankController.cs` | `ddcb4f480189af77efd3d47dc4630f1b844c6a69` |

Original feedback increments `Snapshot.Clear` for an unobstructed weapon ray **without requiring `DamageableSurface`**. The caption explicitly distinguishes no camera hit (`Brak celu`) and a nondamageable hit (`podłoże / osłona`). However central crosshair color is cyan solely on `feedback.Clear > 0`; no `HasSurface` / `DamageableSurface` condition in that color branch. **This is source-proven potential misleading prioritization** (cyan geometry-clear does NOT mean the terrain/target is damageable), not proof of a specific wrong physical projectile hit, and not proof that players actually misunderstand it.

## One executable source-risk gate

```bash
# In a disposable repository checkout with the exact Codex SHA Git object available:
git fetch origin pull/310/head:refs/remotes/origin/codex-310-readonly
node Research/B01-11/PASS14/reticle_signal_source_gate.js .
```

The script only `git show`s the three immutable original C# files, requires their exact Git blob SHAs, then runs eight source predicates. It emits JSON with four derived conditional diagnostic cases. **It does not compile or execute C#, start Unity or emulate PhysX.** Run from the B01-11 research branch; fetching the Codex object changes no checked-out game source.

**Actual B01-11 new-wave source QA:** In the connector V8 runtime, fetched all THREE original file bodies at exact #310 SHA, executed the published `analyze()` logic against those originals: **8/8 source predicates PASS**. Negative mutation in the input HUD source `feedback.Clear>0` → `feedback.Clear>0&&feedback.DamageableSurface` intentionally makes the original-risk detector **7/8, fail-closed**, as expected. **The V8 source checks are not a local Node CLI run, C# compilation or native Unity run.**

## Codex native scenario & evidence — within existing CV-07 / #275

**AIM-U01, nondamageable floor (highest value):** In authorized Unity Editor/Windows Player at 720p and 1080p, vehicle with one working basic gun points camera center at level/sloped **nondamageable ground** 10/30/60m away (within that gun's actual range and elevation). Capture same-frame `LastConsumedAimCollider`, `HasSurface`, `DamageableSurface`, `Guns/InRange/Aligned/Clear`, central crosshair RGB/state, per-gun marker, muzzle forward, camera ray hit, projectile impact and target's health support. **Source predicts:** when ray clear, `Clear>0` yields cyan center even if `DamageableSurface=false`. If physical geometry prevents clear alignment, record that case `NOT_REPRODUCED`, not source fix GREEN.

**AIM-U02, no camera hit:** Point into empty sky/clear horizon with raycast fallback and a gun whose actual range reaches the fallback. Capture `LastConsumedAimCollider=null`, `HasSurface=false`, and crosshair color. This is **conditional** (range/arc may prevent `Clear>0`); do not force fake conditions.

**AIM-U03 positive control:** Within-range stationary damageable target, unobstructed, aligned: show correct positive feedback and actual hit where physically reachable. **AIM-U04 negative:** opaque wall/barrier physically between muzzle and damageable target: show visually distinguished obstruction and actual blocked impact. Include light/heavy, elevation changes and near/far if possible under #275, without making them extra queues.

### Distinct PASS vs FAIL
- **SOURCE gate PASS** means the original mismatch is present and the three blobs are exact; not a gameplay-green claim.
- **Player acceptance PASS after future Codex fix:** distinct user-visible states for **clear trajectory to damageable target** versus **nondamageable ground/cover** versus **no target** versus **actual blocked muzzle**. Cyan may remain a *geometry* cue only if a separate persistent text/icon state makes non-damageability unmistakable; don't paint all situations identical amber because aim geometry and attack viability differ. Preserve existing two-group/multiple-barrel reticles, blocked/out-of-range markers and accurate actual hit physics.
- **Player FAIL:** UI implies confirmed viable enemy hit (same lone affirmative cyan) while `DamageableSurface=false`, without a sufficiently distinctive other cue, or a green promising hit despite a physically blocked barrel. **Physical hit mismatch** is a separate #275 root cause, not automatically this copy/color finding.
- **Evidence required:** exact modified Codex SHA, Unity/Player version+license status, authentic Editor/Player logs, both resolution screenshots/video, one time-correlated telemetry row per tested case including `Surface/ Damageable / Clear / Color`, impact point/collider and result. If environment unavailable: `NOT_RUN/BLOCKED_BY`, never mock native PASS.

## Ownership and next stage

**CodeX FIX_OWNER** for game `Assets/**`; B01-11 only created original Research diagnostic + receiver source checks, on isolated own branch. Do not merge this research branch into integrated game as a gameplay fix. B01-60 independently checks authentic native evidence, B01-11 only evaluates resulting human-facing status. HQ licenses/separate G1 and human GATE D remain unchanged. A new Codex SHA triggers a **differential** acceptance, not repinning old source detector to make it green.
