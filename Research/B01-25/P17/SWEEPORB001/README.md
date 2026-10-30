# B01-25-SWEEPORB001 R1 — moving hazard on original authored rail

B01-25 LEVEL & CHALLENGE DESIGN / PASS 17 WAVE 10. This is **actual executable C#9 gameplay logic**, not a design-only document. It is distinct from static timed HAZARDPULSE #470, two-console SYNCRELAY #448, HOLDOUT #406, and B01-20 ordered CHL checkpoints. B01-25 owns this isolated source folder; Codex owns canonical gameplay and actual Unity integration.

**Mechanic:** In the Telegraph state (1s), the orb waits safely at the rail start; in Moving (2s), its 1m-radius hit circle travels 8m horizontally; in SafeReturn (3s), it rests harmlessly at the end and resets to the start next cycle. The controller receives real host-supplied previous and current world XZ over a delta <= 0.25s and computes **continuous closest relative approach between moving vehicle point and moving orb over the active part of the host interval**. That detects an entire vehicle segment crossing the orb with neither sample endpoint overlapping it. It returns a boolean damage **authorization, once per active cycle**; does not mutate actual host HP or global state. Pause, dead actor, invalid telemetry fail closed; Reset re-arms; phase and exact orb XZ are exposed for host visual telegraph. Orb tracks one point of the robot, not a compound collider.

**Original authored map bindings:** immutable CLEARANCELADDER #359 (7,10)->(9,10), world (28,40)->(36,40), and SLALOM #346 (3,8)->(5,8), world (12,32)->(20,32). All three contiguous cells per rail checked against real immutable MapDocument v2 floor/blocker item data; neither spawn nor finish shares the rail. Map JSON was not edited and the orb is not currently spawned in canonical scenes. Source checks do NOT prove actual physical Heavy/Light clearance or frame-rate behavior in native Unity.

## Reproduce exact real source tests (.NET SDK8)
From repository root with Git installed:
    P=Research/B01-25/P17/SWEEPORB001
    mkdir -p "$P/.output"
    git fetch --no-tags origin ad52a604ffa4f143d98e607049d6f79e0c7e580f f565e250659692541d672cdab108c4cfb8cfdd31
    git show ad52a604ffa4f143d98e607049d6f79e0c7e580f:Research/B01-25/P17/CLEARANCELADDER001/arena.json > "$P/.output/clearance.json"
    git show f565e250659692541d672cdab108c4cfb8cfdd31:Research/B01-25/P17/SLALOM001/arena.json > "$P/.output/slalom.json"
    test "$(git hash-object "$P/.output/clearance.json")" = 53eb696950121465befea2855eda4d7268750016
    test "$(git hash-object "$P/.output/slalom.json")" = 013f5fe2058e76233f580a439e8402c0fe09f124
    dotnet build "$P/SweepUnityCompat.csproj" -c Release -p:LangVersion=9.0 -warnaserror
    dotnet run --project "$P/SweepOrb.csproj" -c Release -- "$P/profiles.json" "$P/.output/clearance.json" "$P/.output/slalom.json" "$P/.output/receipt.json"

Real Windows+Ubuntu GitHub Actions workflow runs the same original source and archives stdout/JSON. **Only claim a PASS after verifying exact HEAD workflow and logs.** C#9/netstandard2.1 source compilation does NOT equal Unity Editor/PhysX PlayMode, real HP, 3D compound collisions, imported .meta GUIDs, gameplay visuals or Player tests. Native Unity G1/G2 NOT_RUN. Strict input contract: vehicle previous and current XZ are assumed linear within host substep; large delta >0.25s rejected; after 10 million simulated seconds clock overflow is rejected.

## Integration boundaries, independent QA and rollback
Source ownership paths: Research/B01-25/P17/SWEEPORB001/** plus .github/workflows/b01-25-w10-sweeporb.yml; no existing Codex gameplay Assets/**, maps, CHL, R2 HAZARDPULSE or garage/physics files modified. B01-40 verifies SHA/manifest/source artifacts/rollback at G0, B01-60 independently retests actual original C#9 source and negative cases; native Unity real vehicle/raycast/HP/VFX tests only under legal authorized Unity environment. B01-00 selects #79 queue after G0; Codex explicit ACK mandatory for any canonical integration. Receiver must create the Unity adapter and apply real host damage through canonical combat system; copy only relevant core + profile after acceptance, not test runner/CI. **No merge, no CODEX_ACK, not integrated.** Rollback is simply discard this isolated source directory and W10 workflow from a disposable branch; future Codex integrated behavior requires separate Codex-owned rollback.
