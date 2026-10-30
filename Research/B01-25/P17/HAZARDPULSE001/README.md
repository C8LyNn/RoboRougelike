# B01-25-HAZARDPULSE-001 / R1 — playable rule engine for telegraphed terrain hazards

**B01-25 Level & Challenge Design** · WAVE 06 · source gameplay package; **NOT imported into the canonical game**. New functionality distinct from B01-25's HOLDOUT-001 (stationary contested extraction) and B01-20's ordered checkpoint course. Original C# implementation, authored real map placements, executable positive/negative source tests, immutable source-map verification, dual-OS CI and rollback. Never edits Codex `Assets/**` nor original map bytes.

## Working gameplay rules (original C#9, not mock code)

- [`PulseCore.cs`](PulseCore.cs): **`PulseZoneController`** emits deterministic `Warning → Active → Recovery` phases in a repeating authored cycle. Warning is never damaging, Active is dangerous only inside the authored circle while the robot is alive, Recovery is safe. `PulseSnapshot.PhaseRemainingSeconds` allows an original VFX/UI countdown without coupling to Unity.
- **Exactly one `DamagePulse=true` authorization per zone activation cycle**, including multiple physics frames and leave/re-enter events within one active window. If the robot remains exposed for 2 seconds, the class does **not** apply per-frame extra damage. The actual damage amount, damage type, armor handling and collision/contact detection are **host-owned Codex integration**, deliberately untouched. This avoids duplicate hits at high FPS, but no actual HP has been modified by this producer.
- `Advance(PulseSample(deltaSeconds, playerWorldX, playerWorldZ, playerAlive, paused))` validates sample before mutation. **Host must substep `deltaSeconds <= 0.25`**; large hitches fail closed instead of skipping entire danger windows. `Paused` freezes the clock and does not emit damage. `Reset()` re-arms the level on a new attempt. `PhaseAt(absoluteSeconds)` supplies read-only phase previews to authored tests.
- Floating-point tolerant inclusive circle boundary (1 nanometer world tolerance) fixed after the real source runner found a false-negative edge at authored 28m + 1.35m. C#9 / netstandard2.1 no `record struct`, no UnityEngine references, no C#10-only methods, no static/global mutable gameplay state.

## Original authored actual level data, NOT a newly cloned arena

[`profiles.json`](profiles.json) attaches two **alternated, nonoverlapping** zones to each of the **actual unchanged** graybox source maps. Grid is 4m per cell; active windows never overlap in sampled eight-cycle source QA.

| Original source map | First hazard | Second hazard | Warning / Active / Recovery |
| --- | --- | --- | --- |
| [CLEARANCELADDER-001 #359](https://github.com/C8LyNn/RoboRougelike/pull/359), original Git blob `53eb696950121465befea2855eda4d7268750016` | cell (7,8) / world (28,32), offset 0 | cell (9,8) / world (36,32), offset 3s | 1s / 2s / 3s |
| [SLALOM-001 #346](https://github.com/C8LyNn/RoboRougelike/pull/346), original Git blob `013f5fe2058e76233f580a439e8402c0fe09f124` | cell (5,8) / world (20,32), offset 0 | cell (9,8) / world (36,32), offset 3s | 1s / 2s / 3s |

All 4 centers were checked on original map **ground-only** cells, avoiding existing item centers, spawn and finish. Source BFS confirms a 4-connected baseline route from spawn to finish even with map walls. The source tests also confirm at least one zone is **not Active** throughout sampled times. These are **static MapDocument-level facts, not true Unity/PhysX clearance or real Light/Heavy driving evidence**. A real obstacle hitbox's collider envelope and camera/UI flashing must be reviewed in Unity after Codex opts in.

## Execute genuine source CI without copying code to the game

Clone the Draft branch and run from the repository root with .NET SDK 8:

```bash
P=Research/B01-25/P17/HAZARDPULSE001
mkdir -p "$P/.output"
git fetch --no-tags origin ad52a604ffa4f143d98e607049d6f79e0c7e580f f565e250659692541d672cdab108c4cfb8cfdd31
git show ad52a604ffa4f143d98e607049d6f79e0c7e580f:Research/B01-25/P17/CLEARANCELADDER001/arena.json > "$P/.output/clearance.json"
git show f565e250659692541d672cdab108c4cfb8cfdd31:Research/B01-25/P17/SLALOM001/arena.json > "$P/.output/slalom.json"
git hash-object "$P/.output/clearance.json"   # expect 53eb696950121465befea2855eda4d7268750016
git hash-object "$P/.output/slalom.json"     # expect 013f5fe2058e76233f580a439e8402c0fe09f124
dotnet build "$P/PulseUnityCompat.csproj" --configuration Release -p:LangVersion=9.0 -warnaserror
dotnet run --project "$P/HazardPulse.csproj" --configuration Release -- "$P/profiles.json" "$P/.output/clearance.json" "$P/.output/slalom.json" "$P/.output/receipt.json"
```

The unique [Actions workflow](../../../../../.github/workflows/b01-25-w06-hazardpulse.yml) executes these **actual original Git source blobs** on `ubuntu-latest` and `windows-latest`; archives stdout and `receipt.json` (original program and map hashes). [Actual runner #41030053845](https://github.com/C8LyNn/RoboRougelike/actions/runs/41030053845) resulted **51/51 PASS per OS**, including 17 negative config/telemetry rejections, both original map SHA pins and C#9/netstandard2.1 warning-as-error compile. Windows physical artifact ID `12964973542`; Ubuntu `12965042464`. Historical real RED runs: #41029766045 (wrong method receiver in test harness) and #41029907433 (genuine IEEE754 inclusive-boundary bug fixed in gameplay core). Any final documentation-only HEAD run must be checked separately; do not assume it passed because the source run did.

## Codex-only integration proposal (NOT executed)

Only after B01-40 independent source/manifest gate and B01-60 independent QA, B01-00 may queue in [CODEX INBOX #79](https://github.com/C8LyNn/RoboRougelike/issues/79), requiring an explicit Codex ACK before the game integration.

```csharp
// In a Codex-owned adapter; not part of the producer source or game yet.
var left = new PulseZoneController(
    new PulseZoneConfig("CLEARANCE_LEFT", 28, 32, 1.35, 1, 2, 3, 0));
// Every actual update, split any dt > .25 into substeps:
PulseSnapshot result = left.Advance(new PulseSample(
    fixedSubstepSeconds, robotPosition.x, robotPosition.z, robotIsAlive, paused));
if (result.DamagePulse) {
    // Codex-owned once-per-pulse damage request ONLY if authorized,
    // applying existing canonical combat/armor policies.
}
switch (result.Phase) {
    // Codex-only warning VFX/sfx, active damage animation, recovery visuals.
}
```

**Dependencies/blockers:** no real host integration, no GameObject/prefab/VFX/material, no real contact/HP, no Unity-generated .meta GUIDs, no camera, no real robot colliders, no native Unity Editor/PlayMode, no Player build/human playtest. Source phase and synthetic point-in-circle do **not** establish a correct game collision/hit registration system. **Unity G1/G2 = NOT_RUN/HOLD**. Real cross-source binding to R3/MapWorld, damage/armor policy, spawn/finish, sound and fail-safe interrupted-run behavior must be decided and owned by Codex.

**B01-40:** verify original 4 hazard centers, scope, hashes, actual physical artifacts, no owner overlap and whether the source-only component qualifies for OFFERED/FROZEN. **B01-60:** independently reproduce exact SHA and negative tests; native scene QA only with authorized Unity 6000.3.14f1 and actual player+hazard/ray/collider visuals, Light/Heavy traversals, warning readability, pause/retry, physical damage once per cycle; record true XML, log and exit.

## Owner-safe rollback

Until accepted, rollback = discard isolated `Research/B01-25/P17/HAZARDPULSE001/**` and `.github/workflows/b01-25-w06-hazardpulse.yml` from a **disposable branch**, clean ignored `.output/`, `bin/`, `obj/`. Original map blobs remain untouched. Once Codex explicitly claims/integrates, any adapter rollback is Codex-owned; do not change game or save source from the B01-25 branch. **Draft PR only, no merge, no direct ACK assertion**.
