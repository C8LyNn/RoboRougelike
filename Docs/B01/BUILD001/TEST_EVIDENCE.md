# BUILD001 R1 evidence ledger

2026-10-25. Source implemented in isolated Draft branch feature/b01-build001-s03-sockets-r1.

Actual new C#:
1. BuildSocketGeometry.cs: patch/face/type geometry and exact 0.55m logical grid.
2. BuildPlacementRules.cs: placement and reachability model, wheel/hub/gun/suspension test catalogue.
3. BuildSocketRaycast.cs: actual RaycastHit spatial face selection.
4. Build001Demo.cs + BUILD001_Demo.unity.
5. Build001SocketTests.cs / Build001PhysicalHitTests.cs (NUnit/UnityTest).
6. Proper asmdefs and .meta; NO R3/P12/Pack/R6 target edits.

Verification boundaries:
- GitHub code committed and package-scoped file/static references: pending independent G0 scan.
- Manual code review spotted PlayMode fixture interference where wheel collider would intercept the core hit ray. Producer fixed test source in commit `10fe52c61fda7501c8edac55c52748a28b5bb5bd`. This is **source test correction**, not a physically executed RED→GREEN.
- Unity 6000.3.14f1 C# compiler: NOT_RUN.
- EditMode NUnit: NOT_RUN; no XML, exit status or native results.
- PlayMode PhysX: NOT_RUN; no XML or real world hit logs.
- Scene manual screenshot/video: NOT_RUN.
- B01-60 independent QA: PENDING.
- B01-40 SHA256/GUID target compatibility: PENDING.
- G1 BLOCKED; G2 HOLD. The existing game's reported wheel attachment issue is NOT YET FIXED in the game; only the new isolated proof-of-concept rules have been written.

## R1.1 producer static rework — 2026-10-25
- Source review found `BuildPlacementRules.Check` could treat a living but **Core-disconnected orphan** as valid structural support. This is a genuine source-level FAIL relative to #140 acceptance.
- Producer changed `Check` to consult `Reachable(installed)`, requiring the supporting part be connected to a living Core. Commit `0c147128a06b3dafc64fe4117cac3effa7914cb4`.
- Added `AliveButCoreDisconnectedFragmentCannotAcceptNewAttachment` EditMode regression source, commit `f49e5a8159fd973bb4253db044dc1b8f74df2a98`.
- **No native RED/ GREEN test was executed.** B01-60 must run this test and all prior ones independently on the FINAL exact SHA; reported statuses stay G1 NOT_RUN and G2 HOLD.
