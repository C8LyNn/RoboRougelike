# MOTION-STALL-001/R1 — passive Unity 6 technical vehicle-stall observation

B01-13 HUMAN PLAYTESTING, PASS17 WAVE10. New, standalone **real C# state machine, Unity IMGUI MonoBehaviour and serialized prefab** for technical facilitator tests. Own scope: Research/B01-13/P17H/** + dedicated workflow only. Baseline main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1. No modification of canonical game code, Codex work, physics, garage, camera, reattach, previous packages or Assets.

## Measured behavior and limits

Receiver assigns a physical observed vehicle root Transform in the optional prefab. Unity adapter **reads only** old Input Manager W/S held state and the root Transform world XZ position. Source-only MotionStallLedger labels **SUSPECTED** stall after >=2 continuous seconds of W/S attempted input and planar speed below 0.10 meters/second, measured by a monotonic unscaled clock. Recovery means >=0.10 m/s planar displacement following that suspicion. Input release cancels a streak without falsely reporting recovery. A sample gap >0.5 seconds or planar teleport >50 meters resets active observation; invalid NaN/infinity/negative/reversed clock rejects. Vertical displacement is deliberately unobserved. Holding W/S does NOT prove the actual game has movement bindings; no physics softlock or vehicle repair is asserted. The panel displays state, suspected stalls, recovered motions, discontinuities, F7 hide/show, reset. Original prefab has observedVehicle unassigned; screen displays an explicit missing-target notice. Input System-only configuration gives an explicit incompatible-input warning instead of false success. No saved observations, identities, network or modifications to any game input/Transform/Rigidbody.

## Exact source QA

Run these commands:

    python Research/B01-13/P17H/verify_package.py
    dotnet build Research/B01-13/P17H/Tests/DotNetOnly/MotionStallTests.csproj -c Release --warnaserror
    dotnet run --project Research/B01-13/P17H/Tests/DotNetOnly/MotionStallTests.csproj -c Release --no-build

CI runs Windows 2025 and Ubuntu 24.04 with .NET8 and Python3.11. 56 original C# behavioral tests compile both actual production scripts against **test-only UnityEngine API mocks** and exercise all state transitions/negative inputs/real adapter Update+OnGUI behavior. Static verifier checks SHA256+byte sizes of 12 source/asset/README/test/workflow files, all 3 stable Unity GUIDs and the prefab script mapping. Test stdout is uploaded as physical CI evidence. This is NOT native Unity G1; tests cannot establish actual Unity 6 compatibility, imported prefab, PhysX behavior or real Player output.

## Authorized Unity 6 test import only

1. Obtain independent B01-40 exact SHA/GUID/rollback gate and B01-60 source test replay. B01-00 must approve test purpose. Do not interpret the source offer as Codex ACK.
2. Only an authorized receiver copies UnityPackage/Runtime and UnityPackage/Prefabs **including their .meta files** into an isolated Unity 6 URP test project, e.g. Assets/B01_13_MotionStall/. Do not copy Tests/DotNetOnly/UnityStubs.cs or change canonical Unity project settings.
3. In that isolated project set Active Input Handling to Old or Both (never in canonical game). Place B01_13_MotionStall_Optional.prefab in a dedicated test scene, **assign observedVehicle to actual vehicle root Transform**, then enter Play. Hold W or S while physically blocked >2 seconds, verify suspicion; clear obstacle and verify recovery; check release/pause/teleport/vertical motion/F7/reset. Confirm game still receives its normal inputs and use a real Unity Player native receipt if actually performed.
4. Roll back: remove prefab scene instance plus entire isolated import folder, verify no scene references remain. Without a real Unity license/host mark all native steps NOT_RUN. No human participants or consent trial is represented; real human studies require HQ GATE D and appropriate consent.

## Independent QA, ownership, restrictions

B01-40 owns G0 manifest, source scope and prefab GUID/rollback. B01-60 owns fresh original source and negative-test replay on the frozen SHA. B01-00 approves technical study methods and future adoption; only authorized Unity/Codex host can validate native G1. No Merge, Codex SENT=NO, ACK=NONE, INTEGRATED=NO. Do not touch immutable B01-13 prior PRs #363/#389/#426/#435/#458/#478. If a receiver requests rework, version a separate R2 without moving a handed-off SHA. Drop this Draft branch for code rollback; separately remove any isolated Unity import.
