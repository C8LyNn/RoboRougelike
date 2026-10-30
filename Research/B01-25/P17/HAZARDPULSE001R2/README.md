# B01-25-HAZARDPULSE-001 / R2 — timestep boundary fix (P17/W09)

**Versioned source-only repair** of immutable original [PR #430](https://github.com/C8LyNn/RoboRougelike/pull/430) commit `6095577ece27320b1a861a738cc23ba42d6793a6`, based on independent original-C# RED [QA #444](https://github.com/C8LyNn/RoboRougelike/pull/444) @ `5dc838f3342083ebc78bbf8704053b996dac4a8a`. Original R1 source and QA remain untouched, NOT green.

## Fix
`PulseCore.cs` normalizes numerical roundoff at warning/active/recovery/cycle-wrap edges to 1e-12 seconds, and uses the same wrap convention for per-activation damage-latch cycle. Unchanged: host substep <= .25 seconds; invalid inputs fail closed; pause/reset/outside/dead prevent unauthorized damage; 1 pulse per active cycle, never actual host HP side effects. This tolerance means an instant within 1 picosecond before an edge is treated as on the edge; it is not a player-facing grace interval.

`Program.cs` retains 51 original game-rule/map cases and adds four producer cases covering decimal partitions 10×0.1 = 4×0.25 and other 1s/3s/6s/7s divisions, wrap re-arm, staggered hazard and no early 1-microsecond phase. `Qa444Regression.cs` is producer's replay of independent #444 assertions against R2; **independent B01-60 retest on this exact new SHA is still mandatory**. Four authored original hazard locations, linked original source map blobs, no changes to gameplay assets/Unity scenes.

## Execute on Windows and Ubuntu with .NET SDK8
From repository root:
```sh
P=Research/B01-25/P17/HAZARDPULSE001R2
mkdir -p "$P/.output"
git fetch --no-tags origin ad52a604ffa4f143d98e607049d6f79e0c7e580f f565e250659692541d672cdab108c4cfb8cfdd31
git show ad52a604ffa4f143d98e607049d6f79e0c7e580f:Research/B01-25/P17/CLEARANCELADDER001/arena.json > "$P/.output/clearance.json"
git show f565e250659692541d672cdab108c4cfb8cfdd31:Research/B01-25/P17/SLALOM001/arena.json > "$P/.output/slalom.json"
test "$(git hash-object "$P/.output/clearance.json")" = 53eb696950121465befea2855eda4d7268750016
test "$(git hash-object "$P/.output/slalom.json")" = 013f5fe2058e76233f580a439e8402c0fe09f124
dotnet build "$P/PulseUnityCompat.csproj" --configuration Release -p:LangVersion=9.0 -warnaserror
dotnet run --project "$P/HazardPulse.csproj" --configuration Release -- "$P/profiles.json" "$P/.output/clearance.json" "$P/.output/slalom.json" "$P/.output/receipt.json"
dotnet run --project "$P/Qa444Regression.csproj" --configuration Release
```
Dedicated Actions workflow archives original stdout and receipt per OS. Actual CI status must be checked at exact new HEAD before claiming PASS. SDK C#9/netstandard2.1 is NOT native Unity compilation/PlayMode/PhysX/HP.

## Handoff and rollback
Paths owned exclusively by B01-25: `Research/B01-25/P17/HAZARDPULSE001R2/**` and unique W09 workflow. Never modify original R1 `Research/B01-25/P17/HAZARDPULSE001/**`, B01-60 QA #444, Codex-owned `Assets/**`, #79 or main. **B01-40** exact SHA/ZIP/GUID/source G0; **B01-60** independent new-SHA QA; **B01-00** queue; **Codex** alone integrates a copied R2 core + profiles into canonical game after explicit ACK, with real robot position/HP, VFX, world collider and real native Unity Light/Heavy tests. No code adapter built here; **G1/G2 NOT_RUN; CODEX_ACK_PENDING; NOT_INTEGRATED**. Rollback: discard just R2 directory and unique W09 workflow on disposable branch; no merge.
