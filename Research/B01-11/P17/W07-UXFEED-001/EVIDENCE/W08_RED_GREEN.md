# W08 — actual reproducible RED -> GREEN: critical HUD event eviction

**Own package:** `B01-11-P17-UXFEED-001/R2` from existing open [Draft #438](https://github.com/C8LyNn/RoboRougelike/pull/438). Historical frozen R1 = `437d27b5ee8c73db4917f4dfcec3363f776c451d`, 87/87 original C# test cases per OS.

## What actually failed
R1 `TacticalFeed.Record()` capped `_history` to 32 items. `Visible()` read only `_history`. If an alert was `WeaponOffline` or `PartDetached` at t=1ms and 32+ ordinary EnemyDestroyed/ObjectiveChanged events arrived at t=2..40ms, the critical alert was evicted from history long before the 6500ms critical TTL. Sorting by priority cannot display a no-longer-present item. This is a deterministic **source-level UX alert loss**, not a claim that the current Unity HUD was tested.

## Genuine RED evidence — source code untouched, only eight new tests added
- Test-only commit `8b909a4c9d9f595f73946945c000dfad7836bf6f`.
- [GitHub Actions #41045734715](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045734715): original compiled C# Ubuntu **91 PASS / 4 FAIL**, Windows **91 PASS / 4 FAIL**.
- Exactly four failing assertions on BOTH OS: critical survives normal-event history overflow; critical-only survives normal flood; two critical messages survive history overflow; latest two critical replace oldest even after flood.
- Four other new checks already passed: normal flooding doesn't manufacture critical alerts, historic cap stays bounded, run reset and TTL still work (though original critical was prematurely missing).

## Genuine GREEN repair
- Original runtime change commit `32514d90feb538b67035f7d5804408170e065e2c`, no force push, old commit unchanged.
- `_criticalVisible` list lives alongside `_history`. Each received critical event enters a separate FIFO capped by `visibleSlots` (1..6). `Visible()` reads critical candidates from this bounded list, ordinary candidates from bounded `_history`, then sorts by priority/newest and respects 6500/3000ms TTL. `StartNewRun()` clears both. No gameplay damage, economy, save, VFX, scene or Unity runtime mutations.
- [GitHub Actions #41045982978](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045982978): original compiled C# Ubuntu **95/95 PASS**, Windows **95/95 PASS**, zero failures; 190 executed assertions. Earlier 87 tests still run.
- Source/native distinction: .NET8 original source compiled; actual Unity game UI/display integration EditMode/PlayMode/Windows Player and human playtest **NOT_RUN**.
- Receiver must independently reproduce at exact R2 HEAD before G0/G1 acceptance. B01-00 controls any #79 handoff. No Codex ACK/integration or merge.
