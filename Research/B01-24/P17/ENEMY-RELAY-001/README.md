# B01-24 / PASS17 — ENEMY-RELAY-001 v1.0.0

**Real new game-content artifact:** alternative six-stage R5 enemy encounter catalog with **10 NEW named encounters**, original `R5Catalog.Stages[].Options[]` serialization, real original enemy archetypes/variants and bounded encounter parameters. This is **optional content** awaiting Codex selection/installation, not an AI source patch and not an LD02 integration or campaign save change.

## Exactly what is in the package

`Encounters-Relay.json` — 6 original-compatible stages (1 + 2 + 2 + 2 + 2 + 1 routes). Route alternatives implement the same `AlternativeRoster` deterministic seed behavior as the existing source `R5RunPlan` (only position-compatible elite alternate rosters). New entries cover light scouting, flanker pincers, bruiser pressure, elite dual-gun teams and a SiegeBoss finale. Every entry uses actual `R3EnemyArchetype` ordinal (Scout=0, Bruiser=1, Gunner=2, Flanker=3), actual `R5EnemyVariant` (Normal=0, EliteBruiser=1, EliteGunner=2, SiegeBoss=3), and actual `R5ArenaKind` (SalvageYard=0, Wasteland=1, Industrial=2).

The original `R1EncounterDefinition.SpawnCount` creates one additional **Normal Scout escort** on non-boss encounters, so a 3-entry `Roster` produces **4 physically spawned enemies**. Bosses spawn **only one**. The original `R1EncounterManager.Spawn` can also add a shotgun/rocket, shield and special equipment to larger squads depending on its original count/health/arena rules; this authored catalog does **not** claim new weapon loadouts or a new battle engine.

## Real reproducible source test

From the repository root (requires .NET 8, no Unity Editor, no NuGet packages):

```bash
dotnet build Research/B01-24/P17/ENEMY-RELAY-001/EnemyRelayP17.csproj -c Release --warnaserror
dotnet run --project Research/B01-24/P17/ENEMY-RELAY-001/EnemyRelayP17.csproj -c Release
```

The `.csproj` directly compiles **three original, unchanged C# files** from this exact repository: `Assets/R5/R5RunPlan.cs`, `Assets/R1/R1RunDefinition.cs`, `Assets/R3/R3EnemyArchetype.cs`. Headless `UnityTestStubs.cs` substitutes only `UnityEngine.JsonUtility` with `System.Text.Json`, `Mathf` and Unity annotations. These are expressly **not** proof of Unity serialization/import parity. The runner executes original `R5RunPlan.Validate`, `Threat`, generated seed `Offers`, original spawn-count and escort logic, positive controls against the unchanged published catalog and negative mutation tests for duplicate IDs, incompatible elite/alternative rosters, missing stage, invalid range, invalid budget, malformed options and rewards.

A dedicated Windows+Ubuntu CI workflow `.github/workflows/b01-24-p17-enemy-relay.yml` runs exactly those source-contract tests and archives raw console receipts. Original native Unity EditMode, PlayMode, PhysX, actual R6 interactions and Windows Player are all **NOT_RUN** until Codex performs them with a genuinely authorized runner.

## Codex optional import only — and exact rollback

**Do not cherry-pick this research branch into the running game wholesale.** On explicit Codex claim and HQ acceptance in a disposable Unity checkout:

1. Check present game SHA, inspect `manifest.json` source inputs, preserve the exact existing `Assets/Resources/R5/Encounters.json` contents and its `.meta` file.
2. Replace **only the contents of that JSON file** with `Encounters-Relay.json`; do not replace the original `.meta` GUID. `R1RunController.Start()` already loads the canonical `Resources.Load<TextAsset>("R5/Encounters")` and calls `R5RunPlan.Validate` on startup. No extra enemy spawner is proposed or required.
3. Run Unity 6000.3.14f1 native **actual `JsonUtility.FromJson<R5Catalog>`**, run plan construction for seeds 7, 31, 83, 109, 202, 407, validate source `SpawnCount` and boss handling, all six stage transitions, actual four-enemy encounters, defeat and once-only rewards. Record XML, Editor.log, original source+asset SHA and exit; test with real player after scripted checks.
4. Restore original JSON bytes in the disposable checkout from a **verified backup or the same canonical commit**, without overwriting unrelated/uncommitted game changes. The original `.meta` remains unchanged; rollback requires no new runtime components.

**Known limitations / owner decisions:** The current LD02 alternative chooser rejects 4-enemy rosters (`SpawnCount>3`), so not every stage/route is eligible for LD02. Balancing and rewards are new optional values, **not approved campaign rebalance**. No custom enemy-specific spawn positions are encoded; the existing `R1EncounterManager.Spawn` controls positions. Nothing in `Assets/**`, CHL or PARTSIZE is changed. The package is `DRAFT / CODEX_ACK_PENDING / NATIVE_NOT_RUN`. B01-40 performs intake, B01-60 independent QA, Codex owns actual integration and rollback.

**Provenance:** [manifest.json](manifest.json) includes original C# Git blob IDs and every new file Git blob, so an integrator can detect stale source or mutated data. See actual Actions run and Draft PR for exact results.
