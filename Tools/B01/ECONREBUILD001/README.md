# ECON-REBUILD-001 — B01-22 independent P1 Unity source

Authorized autonomous B01-22 source-only production. Canonical owner rules [#136](https://github.com/C8LyNn/RoboRougelike/issues/136), economy roadmap #137 and RUN-SAFETY #154. **This package does not integrate with the live R3 economy, never creates new inventory, never edits active Codex worktree, and does not implement temporary loaner/anti-softlock selection**.

## Runtime contract
- `Assets/B01Packages/ECONREBUILD001/Runtime/RebuildQueueService.cs` pure Unity C# service, separate EditMode NUnit tests, asmdefs, stable .meta GUIDs. `IAuthoritativeRebuildPort` observes existing live R3 owned parts and delegates all wallet/HP mutations to a **single atomic host authority**. No duplicate `R3PartState` list.
- Only genuinely destroyed owned parts at **0 HP** can enter rebuilding. Alive but damaged parts continue ordinary instant R3 repairs (not touched here). A reserved part is *REBUILDING and must be unavailable* for deploy, mounting, shop/sell and reward routes; receiver must enforce this at **all** native entrypoints.
- Exactly ONE free reserved rebuild per consecutive genuine victory epoch; any additional reservation in that same epoch pays a discounted quote validated as `0 < quote < purchase price`. 65–75% is a **balancing hypothesis**, NOT APPROVED. No "free slot refill" from garage restart, scene restart, seed change, defeat or elapsed wallclock time.
- All reservations in the current epoch finish after the NEXT distinct genuine winning challenge. Loss, arena/garage, duplicate victory events or failed atomic host batch change no HP/progress. Atomic batch is mandatory; a partially successful host operation causes fail-closed quarantine and requires manual resolution, not blind retry.
- Stable physical instance IDs (lowercase canonical nonempty GUID). Pending metadata and victory idempotency ledger `JsonUtility` sidecar. Host physical parts/reservations/wallet and sidecar MUST be persisted/restored as a **single** crash-consistent transaction; restore host before `TryRestore`. Service doesn't assert that host implements this automatically.
- No R3 `Repair()` call from queue, since it charges and heals immediately; actual host must provide reserve+fee+complete atomic functions. No unapproved cost, timer, free-spawn or hidden asset restoration.

## Evidence/reproduce
```bash
dotnet build Tools/B01/ECONREBUILD001/tests/EconRebuild.SurrogateTests.csproj -c Release
dotnet run --project Tools/B01/ECONREBUILD001/tests/EconRebuild.SurrogateTests.csproj -c Release --no-build -- artifacts/econ-rebuild/test-results.json
python3 Tools/B01/ECONREBUILD001/build_package.py artifacts/econ-rebuild
```
.NET8 compiles **actual Unity package C#** with explicit non-native `UnityEngine.JsonUtility` shim, tests deterministic queue. Native NUnit EditMode is source authored, Unity 6000.3.14f1 G1 **NOT_RUN** unless licensed test evidence exists. CI creates deterministic ZIP and per-file source SHA manifest with exact Git SHA, rooted `Assets/B01Packages.meta` matching MAPGEN/LEGENDARY's canonical GUID.

## Adapter dependency and integration
See `R3EconRebuildPort.install.cs`: read-only R3 bridge over existing `R3VehicleModel.Parts` + `Inventory`, `Price`. Unimplemented (and **explicitly rejected**) authoritative `IAtomicRebuildR3Hooks` provides reservations, durable debit, host ownership block and same-win atomic batch completion. This cannot be made safe using current R3 public `Repair`, `RepairInventory` or `Remove` directly.

**Minimal Codex tasks after B01-00 G2:** implement authoritative atomic R3 hooks/wallet journal/sidecar snapshot and genuine activity WIN trigger; enforce `IsRebuilding` through assembly/deploy/sell/loot; coordinate anti-softlock choice #154; run licensed Unity EditMode/PlayMode, UI and Windows R6 physical tests. Avoid duplicate purchase/repair expense, preserve ID/level/modifiers, don't resurrect with free instant HP.

## Rollback
Use B01-40 PACKAGE-AUTO staging in a disposable worktree with SHA/manifest/GUID verification. Back up save/wallet and event journal; remove only ECONREBUILD001 package+specific receiver hook, keep parent B01Packages meta, undo host routing, restore baseline saves; if any queued tickets still exist, **do not delete journal or force-load without migration**, quarantine and explicit user choice. G0 independent PENDING; G1 Unity NOT_RUN; G2 HOLD / NO MERGE.
