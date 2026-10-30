# B01-25 / PASS17 WAVE24 — SAFEROUTE001 R2 | LIVE-POSITION ROUTING

**One immutable original R1 parent** [PR #745 @ de59e8ce7d5e98958874c738947f788846f3583a](https://github.com/C8LyNn/RoboRougelike/pull/745). W24 R2 is NEW separately versioned *successor branch* based on that exact original immutable SHA. NO edits to frozen R1 branch, original Save/Load #510/#556/#690/#729/#712, canonical game or other producers. This PR is **stacked on R1** and MUST NOT be installed alongside R1, because they share identical Unity asmdefs and 13 GUIDs by design. Receiver chooses R2 AS THE SOLE source version only after exact SHA source G0.

## Concrete confirmed R1 gameplay bug / fix (not another gadget)

W23 R1's `Replan()` used `SafeRoutePlanner.Find(level,...)`, which always began at static `map.Start` even after real Rigidbody vehicle traversed to another cell. As a result the UI rendered obsolete starting waypoints and could mislead players. R2 changes the original pure C#9 shortest-route engine with a backward-compatible `SafeRoutePlanner.FindFrom(map, actualCell, footprintRadius, gateHazardState)`. `Find()` still routes from design spawn for map static acceptance; `FindFrom()` fails closed for outside-map, active hazard, closed gate, or heavy footprint too large. New runtime `ActualPlayerCell()` converts real Rigidbody world XZ into exact map cells; fixed-update triggers live replan on observed cell transition (plus existing gate/hazard changes, reset/Light-vs-Heavy swap). `Replan()` now builds cyan waypoints from **actual current cell** to map Exit instead of old SPAWN and exposes `RouteOrigin` and `CurrentRouteFromPlayer` for Unity diagnostics.

Another R1 safety issue: `PhysicallyClearCurrentPlan` conditionally ignored Unity gate colliders whenever the script reported `physicalGateClear=true`. R2 ALWAYS rejects any genuinely overlapping gate `BoxCollider` even if gate script marked all gates open, reducing stale/false-positive gate completion risk.

**R2 original source regressions**: +13 C#9 tests, including player-in-motion, from Exit, 8 dynamic Light/Heavy+Gate+Hazard states, no shared result mutation, gate/hazard-origin fail closed, out-of-bounds teleport and malformed route inputs. **Eight native Unity6 PlayMode authored tests** (six preexisting plus two real physical current-position tests), all G1 NOT_RUN until actual licensed Unity 6000.3.14f1 Windows runner yields process exit and NUnit3 XML+Editor.log. Original pre-R2 50 C#9 cases still included (63 total expected). No actual Unity physics/PfX execution or user-world installation in this chat.

## Source-only isolated manifest and safe receiver

R2 branch based exactly on parent R1 original SHA; diff should change **only FIVE owner-scoped existing original C#9/Unity files** and add **FIVE new R2-specific files** (own checker, README, .gitignore, manifest, unique W24 CI workflow). The old R1 `Tools/B01/B01-25/P17W23SafeRoute/manifest.json` remains immutably pinned to R1 and **IS NOT A VALID MANIFEST FOR R2**. B01-40 MUST read ONLY R2's `Tools/B01/B01-25/P17W24SafeRouteR2/manifest.json` for original current source bytes, full 21 Unity source+meta paths, 13 stable GUIDs, 3 asmdefs. W24 physical SOURCE ZIP includes 21 original Unity source+meta files + own embedded `PACKAGE_MANIFEST.json`, and is NOT a Unity Player. The R2 manifest pins all 35 original external source files, excluding itself and obsolete R1 manifest. It independently validates exact 5 parent blob SHAs against the immutable R1 Git source.

Run from branch checkout with .NET8, Python3, Git (no Unity required for these):

```bash
dotnet build Research/B01-25/P17/SAFEROUTE001/SafeRouteRuntime.csproj -c Release -warnaserror
dotnet run --project Research/B01-25/P17/SAFEROUTE001/SafeRouteTests.csproj -c Release -- Research/B01-25/P17/SAFEROUTE001/.output/results.json
python Tools/B01/B01-25/P17W24SafeRouteR2/check.py --out Tools/B01/B01-25/P17W24SafeRouteR2/.output/receipt.json
```

GitHub Actions executes exact-source C#9 project and independent original Git hash/meta/asmdef/physical ZIP/safe rollback tests on Windows+Ubuntu; third job downloads raw ZIPs, computes physical SHA256 and byte `cmp`. **Only label producer source green once actual logs for the exact final SHA succeed**. B01-40 performs formal G0 separately, B01-60 runs actual Unity native G1 or keeps NOT_RUN, and B01-00 decides future exact version integration. No auto-merge, no Codex ACK, G2 HOLD.

**Versioned install:** clean disposable checkout only, never modify existing R1 files in same project or overwrite frozen assets. The receiver can reconstruct R2 from full original source archive, but MUST refuse any existing same path and stage new paths only. Reversible rollback unlinks only newly staged files; preserve foreign files, empty folders, symlinks and old R1. Stacking PR on R1 is purely Git ancestry, NOT coinstalling duplicate versions.
