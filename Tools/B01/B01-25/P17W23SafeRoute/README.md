# B01-25 — PASS17 / W23 — SAFEROUTE001 R1

## Actual new functionality — dynamic level traversability, not a fourth single gadget

SAFEROUTE001 provides an original conservative **Light/Heavy 2D level safe-route audit and live Unity6 physical scenario builder**, to reduce softlocks and help actual game integrators accept obstacles. It does not modify MAPGEN #93 or own procedural seed generation (B01-21). Original pure C#9 planner parses a bounded 15x11 character grid with 3 columns of dynamic gate cells and 2 hazard cells, validates exactly one spawn/exit and rejects malformed/unbounded layouts. Four-neighbor deterministic BFS checks SQUARE physical footprint clearance: Light radius0 (one cell), Heavy radius1 (3x3 clear grid cells). A dynamic gate is impassable until truly opened, hazard cells impassable while active. The **eight** scenarios (both rigs x gate open/closed x hazard active/inactive) MUST have a path. Current original scene includes a real static bypass under the gates, so no Light/Heavy softlock even if all gates are closed and the hazard active. The planner independently returns an actual ordered shortest route witness and bounds exploration to the number of grid cells. Explicit negative tests reject maps with disconnected exits or too narrow Heavy passages.

The standalone Unity6 host **ACTUALLY constructs** a playable 15x11 set of physical BoxCollider walls, three separate raised/lowered kinematic Rigidbody doors, a real floor, and two real physical repulsion pads. It spawns Light=1.5kg or Heavy=12kg gravity-driven physical CapsuleCollider Rigidbody. The physical obstacles exist, dynamic gates rise with Rigidbody.MovePosition, the hazard surface uses actual Collider.Raycast and Rigidbody.AddForce while active, and human IMGUI controls provide steering/route state toggles. Cyan route waypoint markers are shown with their colliders disabled; selection only switches from closed to open gate state after actual physical gate bodies reach conservative clearance. The diagnostic `PhysicallyClearCurrentPlan()` makes genuine Unity `Physics.OverlapBox` probes against exactly the walls/gates for the current route witness, not only data rows. The planner remains a **conservative designer-level safety certificate**, not proof of complete PhysX vehicle movement, suspension/contact manifolds, jumping/physics impulses or 100% blockage-free player path. G1 remains NOT_RUN until licensed real Unity runs it.

## Exact isolated source and tests

Only NEW source paths: `Assets/B0125SafeRoute001/**` plus root .meta (21 original Unity code/meta paths with 13 unique GUID, 3 isolated asmdefs), `Research/B01-25/P17/SAFEROUTE001/**` (6 original C#9 source/test/project files), `Tools/B01/B01-25/P17W23SafeRoute/**` (QA checker, README, manifest and .gitignore) and dedicated unique W23 GitHub Actions workflow = 32 brand-new Git paths. Self-excluding manifest pins all 31 other exact original Git blob SHAs. The original C#9 `SafeRouteCore.cs` and `SafeRouteExample.cs` in Unity MUST be byte-identical to research sources. Six original native Unity6 NUnit PlayMode TEST SOURCE cases provided but none executed in Unity here.

The Unity Editor menu `B01/PASS17 W23/Create SAFEROUTE001 Standalone Scenario` creates a NEW additive isolated `.unity` scene on authorized activated Unity 6000.3.14f1 only, refusing any existing scene path. That `.unity` is NOT already generated/imported/tested. Code has no canonical Save/Load, R5, Codex game or previous B01-25 package dependencies.

Actual original Windows/Ubuntu source test invocation with .NET8, Git, Python3:

    dotnet build Research/B01-25/P17/SAFEROUTE001/SafeRouteRuntime.csproj -c Release -warnaserror
    dotnet run --project Research/B01-25/P17/SAFEROUTE001/SafeRouteTests.csproj -c Release -- Research/B01-25/P17/SAFEROUTE001/.output/results.json
    python Tools/B01/B01-25/P17W23SafeRoute/check.py --out Tools/B01/B01-25/P17W23SafeRoute/.output/receipt.json

Unique GitHub workflow runs actual C#9 source behavior tests (all 8 combinations, positive and malformed cases) and independent 17 original Git SHA/Unity .meta GUID/asmdef/native-asset-ownership/safe scratch rollback checks on both OS. A third job downloads both original 22-entry physical Unity SOURCE ZIP archives (21 original Unity source/meta files plus PACKAGE_MANIFEST.json), independently computes SHA256 and verifies actual byte `cmp`. ZIP is SOURCE not Unity Player, CI green is NOT an activated Unity native PASS.

## Receivers, blockers, non-overwrite, rollback

**B01-40** independently verify all 31 original per-file Git blob pins / 13 distinct GUIDs / three asmdefs / 22-member original ZIP, safe disposable receiver staging with existing foreign empty dir/files/symlinks; formal SOURCE G0 PENDING. **B01-60** on user-authorized Windows Unity6 native runner #667/#711 independently executes actual six PlayMode sources and returns full original NUnit XML + Editor.log + exit or G1 NOT_RUN. **B01-00** eventually decides Codex integration after gates, no fake OFFER/ACK/G2. Current `SOURCE_G0=PRODUCER_PENDING / FORMAL_G0=PENDING / NATIVE_G1_NOT_RUN / CODEX_ACK_NONE / G2_HOLD / NOT_INTEGRATED`. No main/develop merge, no physical installation on user's machine, no live game acceptance.

Explicitly not touching selected canonical #729, storage #712/#669, host #690, frozen #510/#556/#596 or ALT_HOLD #719/#693/#682/#695. No alternative Save/Load or private world API; B01-25 level-only independent feature.

Rollback **only unlinks own newly imported files in disposable checkout**. Does not recursively remove existing shared directories, foreign empty folders, symlinks, already-existing content, other owners or Codex assets.
