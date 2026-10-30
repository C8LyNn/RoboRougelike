# B01-25 SYNCRELAY-001 / R1 — unordered dual-relay time puzzle

**Owner:** B01-25 Level & Challenge Design, WAVE07. Real C#9 gameplay module and two actual authored map placements; no canonical game code changes. Distinct from HOLDOUT contested extraction, HAZARDPULSE damage zones and CHL ordered checkpoints.

## Game behavior

The actual [SyncRelayCore.cs](SyncRelayCore.cs) supplies SyncRelayController, SyncRelayConfig, RelaySite, SyncRelaySnapshot. Players can activate either console first within a finite interactive radius; that press starts a visible time window. The second, distinct relay must be activated before the timer reaches zero, unlocking once. Repeated presses of the first console cannot unlock, expiry returns to Idle so the player may retry (no internal softlock), Reset re-arms, death clears even a completed state, and pause freezes clock and blocks interactions. Coordinates and timing are validated strictly before mutation, with a tiny IEEE754 tolerance at inclusive radius. The source never applies combat damage, changes global state, changes run saves, affects camera or intercepts existing Finish.

[profiles.json](profiles.json) contains original data for four optional console locations in two immutable authored MapDocument v2 arenas:

| Source map | Relay A | Relay B | Timer | Static floor shortest between relays |
| --- | --- | --- | --- | --- |
| CLEARANCELADDER #359; original Git blob 53eb696950121465befea2855eda4d7268750016 | cell (9,6), world (36,24)m | cell (5,10), world (20,40)m | 24 seconds | 8 grid cells = 32m |
| SLALOM #346; original Git blob 013f5fe2058e76233f580a439e8402c0fe09f124 | cell (5,6), world (20,24)m | cell (9,12), world (36,48)m | 48 seconds | 16 grid cells = 64m |

All four centers verified Ground-only and devoid of non-Ground item at the exact cell; original map spawn, both relays (in either order), and finish are accessible over the source 4-connected no-wall floor grid. At no point is this proof of actual vehicle drive or collider clearance. The game owner must assess real Light and Heavy wheels, dynamic objects and physics.

## Reproduce original source tests

Requirements: Git checkout of EXACT proposed SHA; .NET 8 SDK and ability to fetch the immutable Git objects from origin. From repository root:

~~~bash
P=Research/B01-25/P17/SYNCRELAY001
mkdir -p "$P/.output"
git fetch --no-tags origin ad52a604ffa4f143d98e607049d6f79e0c7e580f f565e250659692541d672cdab108c4cfb8cfdd31
git show ad52a604ffa4f143d98e607049d6f79e0c7e580f:Research/B01-25/P17/CLEARANCELADDER001/arena.json > "$P/.output/clearance.json"
git show f565e250659692541d672cdab108c4cfb8cfdd31:Research/B01-25/P17/SLALOM001/arena.json > "$P/.output/slalom.json"
git hash-object "$P/.output/clearance.json"  # expect 53eb696950121465befea2855eda4d7268750016
git hash-object "$P/.output/slalom.json"    # expect 013f5fe2058e76233f580a439e8402c0fe09f124
dotnet build "$P/SyncRelayUnityCompat.csproj" --configuration Release -p:LangVersion=9.0 -warnaserror
dotnet run --project "$P/SyncRelayTest.csproj" --configuration Release -- "$P/profiles.json" "$P/.output/clearance.json" "$P/.output/slalom.json" "$P/.output/receipt.json"
~~~

[Actual Actions #41037968021](https://github.com/C8LyNn/RoboRougelike/actions/runs/41037968021) both Windows/Ubuntu SUCCESS on real original source; 53/53 tests per OS including 15 explicit negative rejections; C#9/netstandard2.1 warnings-as-errors compilation; both original map Git inputs checked. Physical unexpired stdout+JSON receipt ZIPs: Windows artifact 12969460610, Ubuntu 12968474000. This confirms producer source G0 only, NOT independent G0 or native Unity G1. Later documentation commits must be checked on their own HEAD.

## Codex-only integration contract

Codex may opt in to the standalone module after B01-40 independent verification and B01-60 independent QA, scheduled by B01-00 in #79. ONLY reviewed SyncRelayCore.cs and selected profile may be adapted into Codex-owned canonical game. Codex must instantiate one SyncRelayController for the intended level, create actual Unity console prefabs and trigger input, supply authoritative robot world-XZ/alive and pause, call Activate(siteId, x, z, alive, paused) on actual interaction, call Advance(Time.deltaTime, alive, paused) per frame, and use BecameUnlocked for an explicitly approved optional gate/Finish behavior. Existing Finish must behave identically when puzzle mode is off. Reset on a new run. The module cannot verify a real Unity raycast or Input System event, save/load, UI/HUD feedback, sound, damage, enemy AI or route obstacles.

### Acceptance boundaries / blockers

B01-40 verifies 10-path producer scope and manifest/physical artifacts/rollback and independently decides OFFERED_FROZEN. B01-60 reruns original code/tests and validates a native authorized Unity 6000.3.14f1 scene and both real vehicle rigs before G1, recording Editor/Player XML+logs+exit. No active Unity environment, GameObjects, colliders, door, save, compiled Player or human trial was used here: **UNITY_NATIVE_NOT_RUN, CODEX_ACK_PENDING, NOT_INTEGRATED, NO_MERGE**.

Rollback before owner claim removes only Research/B01-25/P17/SYNCRELAY001 and its uniquely scoped workflow in disposable branch and cleans ignored .output/bin/obj. After Codex claims a SHA, B01 does not edit the claimed version; Codex owns integration and rollback.
