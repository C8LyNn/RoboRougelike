# B01-25-HOLDOUT-001 / R1 — optional contested extraction objective

**Producer:** B01-25 Level & Challenge Design. **Runtime deliverable:** a genuinely executable, self-contained, pure-C# (net8) level challenge state machine, **not another arena** or a replacement for B01-20 CHL ordered checkpoint track. Neither enemy AI nor combat damage nor rewards/run-save are implemented or modified. Original parent maps and canonical `Assets/**` remain untouched. The host Codex gameplay must opt in before HOLDOUT changes victory.

## Game mechanic in this actual package

- `HoldoutCore.cs`: reusable `HoldoutController`, `HoldoutConfig`, `HoldoutSample`, `HoldoutSnapshot`; accepts `Tick(deltaSeconds, playerXZ, hostileCount, alive)` via a typed telemetry sample. 5 phases: Waiting/Charging/Contested/Recovering/Ready; idempotent one-shot `BecameReady` transition and latching while the robot remains alive.
- Player alive + inside zone + hostile count 0: charge toward configurable required seconds. Hostile present: **freeze** progress, no advance. Outside: **decay** progress by an authored multiplier, floor at zero. Dead: reset progress and readiness **even if already ready**. Reset from run restart is explicit. No permanently uncompletable internal state: a live player re-enters and may recharge after retreat/death reset, with zero hostiles.
- Strict numeric and hostile-count validation before mutation (negative/NaN/infinite deltas and positions fail closed); inclusive physical radius edge; deterministic fractional dt under unchanged telemetry; no global event handlers/state or engine references.
- `profiles.json`: **TWO** authored objective placements in existing immutable real arena data, not two new arenas. `CLEARANCE_HOLD` center cell (9,10) = (36m,40m), radius 2.5m, charge 8s. `SLALOM_HOLD` cell (7,11) = (28m,44m), radius 2.5m, charge 10s. Both live on verified unoccupied `Ground` cells and are situated before the original `Finish` marker. Outside decay multiplier 1.5 gameplay seconds/sec. They are optional and do not edit source JSON or create real checkpoint triggers.
- `Program.cs` + `Holdout001.csproj`: runnable original C# test/player-telemetry replay and invalid-input regression (no borrowed game implementation); exact SHA check of original arena blobs. CI compiles and executes same code on Windows and Ubuntu and archives real JSON receipts + stdout. `profiles.json` is data used by the actual source executable.

## Reproduce source tests

From PR branch clone with .NET 8 and network Git origin:

```bash
export P=Research/B01-25/P17/HOLDOUT001
mkdir -p "$P/.output"
git fetch --no-tags origin ad52a604ffa4f143d98e607049d6f79e0c7e580f f565e250659692541d672cdab108c4cfb8cfdd31
git show ad52a604ffa4f143d98e607049d6f79e0c7e580f:Research/B01-25/P17/CLEARANCELADDER001/arena.json > "$P/.output/clearance.json"
git show f565e250659692541d672cdab108c4cfb8cfdd31:Research/B01-25/P17/SLALOM001/arena.json > "$P/.output/slalom.json"
git hash-object "$P/.output/clearance.json" # expect 53eb696950121465befea2855eda4d7268750016
git hash-object "$P/.output/slalom.json" # expect 013f5fe2058e76233f580a439e8402c0fe09f124
dotnet run --project "$P/Holdout001.csproj" --configuration Release -- "$P/profiles.json" "$P/.output/clearance.json" "$P/.output/slalom.json" "$P/.output/receipt.json"
```

The CI workflow `.github/workflows/b01-25-w03-holdout.yml` runs this command on Windows+Ubuntu with real dotnet 8, records exit and stdout and fails if required receipt missing. Exact final run IDs/results belong in `evidence.json`; do not infer native Unity execution from this CI.

## Codex integration API (proposal only; requires owner acceptance)

```csharp
var rules = new HoldoutConfig("CLEARANCE_HOLD",36,40,2.5,8,1.5);
var objective = new HoldoutController(rules);
// From the actual Unity loop (Codex owns this adapter):
var result = objective.Tick(new HoldoutSample(Time.deltaTime,
    playerPosition.x,playerPosition.z,hostilesActuallyInZone,playerIsAlive));
if (result.BecameReady) { /* Codex-owned optional extraction authorization */ }
// On new run / level restart: objective.Reset();
```

**IMPORTANT:** `hostilesActuallyInZone` must be calculated by the actual Codex game using its real authoritative enemy state; this module deliberately does not spawn, count, damage, detect or move enemies. It does not intercept the existing `MapFinish` trigger or guarantee that ordinary Finish waits for this objective. Codex must decide opt-in victory gating, objective messaging, enemy contest definition and save semantics. B01-24 owns enemy logic; B01-10 balance; B01-20 CHL checkpoint course; B01-40 integrations. Do NOT copy `Program.cs`, CI, JSON receipt, or backup tooling into canonical gameplay; only a reviewed copy of the standalone source `HoldoutCore.cs` and selected authored profile config if accepted.

## QA acceptance / native blocking

**B01-40:** verify exact own scope and hashes, duplicate avoidance against CHL checkpoints and B01-24 enemy formations, no `Assets/**` changes, load receipt and distinguish producer PASS from independent acceptance. **B01-60:** independently rerun both OS and inspect boundary, contested freeze, outside decay, invalid telemetry, completion one-shot, death after ready and restart Reset. **Unity G1** requires actual Unity 6000.3.14f1 Edit/PlayMode test with the real robot entering/leaving zone, the game actually counting nearby enemies, and the real Finish integration branch if Codex opts in. Real collider, FrameTime, camera, hostiles and PlayMode exit/XML/log. Neither source CI nor JSON alone grants G1/G2.

**Rollback:** remove only isolated producer directory and its unique workflow/branch in a review/test clone; generated ignored `.output`, `bin`, `obj` can be deleted independently. Future integration removal or adapter repair is Codex-owned and requires explicit agreed version, not a producer edit to live game source. Existing arena data remain immutable. **State:** SOURCE_EXECUTED / INDEPENDENT_QA_PENDING / UNITY_NOT_RUN / CODEX_ACK_PENDING / NOT_IN_GAME / NO_MERGE.
