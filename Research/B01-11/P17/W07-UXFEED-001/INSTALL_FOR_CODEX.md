# UXFEED-001 — safe future integration, Codex-only after HQ offer + explicit ACK

## Read-only importer/source test
```sh
git fetch origin research/b01-11-p17-w07-uxfeed-001
git worktree add --detach /tmp/b01-11-uxfeed origin/research/b01-11-p17-w07-uxfeed-001
dotnet run --project /tmp/b01-11-uxfeed/Research/B01-11/P17/W07-UXFEED-001/Tests/TacticalFeedTests.csproj -c Release
```
No producer modifications to an existing Codex worktree.

## Host contract and UX display
Only Codex, after B01-00-approved source OFFER and explicit CODEX_ACK, may import `Runtime/TacticalFeed.cs` into its own game UI assembly. Host must emit **real authoritative events**, never infer a destroyed part from generic loss of core HP, and must not award salvage via this UI feed. Map current game run epoch to a strictly increasing `RunId`. On new run call `StartNewRun(id)`. Host feeds monotonic time in integer milliseconds via `AdvanceTo`/new `Record` calls and strictly increasing per-run `Sequence`. Suppress duplicate callback delivery at the adapter, as duplicate sequence is rejected, not silently counted.

`TacticalFeed.Visible()` provides top priority events for optional visual HUD text+icon rendering; `Visible(false)` is critical-only mode. `History()` returns a bounded chronological list for a later read-only accessibility/log screen. The returned records deliberately expose **typed kind, safe subject token and aggregated quantity** rather than any localized string or untrusted dynamic markup; Codex UI owns actual PL/EN game terminology, contrast, focus and screen-reader support. This does **not** replace UXHELP's persistent critical garage hints or Codex's existing hit visuals.

**Native QA required:** original event hook fidelity for actual death/destruction and reward events; screen priority under burst; pause time handling; 720p/1080p/4:3 GUI footprint; full Player run reset, no stale old-run messages and no GUI overlap. These are NOT_RUN by B01-11 source CI.

## Rollback
Ignore/delete isolated producer W07 research branch and its own workflow. If a canonical Unity adapter is later approved, only Codex rolls back the game-side integration; no B01 runtime, scene, prefab, .meta, R3, save, economy, or VFX code is modified here.

## W08 R2 critical-alert retention boundary
The 32-event chronological `History()` and the bounded (<=6) critical-alert transient view are now separate. Integration must never infer critical alert dismissal from history eviction; `Visible()` is the sole current TTL/priority view. New run clears both. Do not make persistent garage blocker copy or actual player damage depend on this UI feed. Source change is limited to `TacticalFeed.cs`; game-owned adapter, HUD geometry and VFX remain Codex territory. [RED/GREEN](EVIDENCE/W08_RED_GREEN.md).
