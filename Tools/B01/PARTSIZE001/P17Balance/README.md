# B01-23 / PARTSIZE001-P17-MASSBALANCE-001 R1

## What runs (actual source, not a GDD)
Pure C# static center-of-mass and ground-contact **support polygon / ideal planar tipping margin**. Callable from Unity 6 runtime or a .NET8 source test without writing any Codex garage, R3 prefab, combat, economy, save, canonical assembly or original frozen P17 data.

API: `B01.PARTSIZE001.P17Balance.MassBalanceSolver.Evaluate(parts, contacts)`. Each `PartMass` must have an **actual supplied** positive kg and center position in meters, and each `GroundContact` must have an **actual grounded support location**, e.g. observed real wheel contact point. Coordinates share one local/world frame. The return includes weighted CoM, convex contact hull count, signed shortest edge distance (positive inside, negative outside), and theoretical plane tip angle. Yaw helper supports 0/90/180/270.

The solver **refuses** missing/nonfinite masses, invalid/duplicate IDs, noncoplanar contacts beyond 5 mm, fewer than 3 non-collinear contacts, overflow, CoM not above support plane, >180 part owners or >64 grounded points. This avoids a green fictitious stability result when physical data has not been measured. Collinear hover/two-wheel vehicle states are UNKNOWN, not safe.

## Limitations / required receiver work
- THIS IS AN IDEAL STATIC PLANAR ESTIMATE, NOT RIGIDBODY STABILITY, COLLIDER FIT, SLIP, suspension, friction, wheel torque, actual damage, typed sockets, or Unity G1 acceptance.
- No actual wheel/track/hover surface contacts, mass distribution or part local center samples for the frozen 3 P17 rigs have been measured; **do not derive those from grid-only JSON**. Never label a simulated point as real contact.
- No canonical Codex integration or preview/garage modification authorized. Original PARTSIZE #286 / rigs #348 / weakpoints #361 / braces #375 / ZIP #391 / native probe #405 are not changed.

## Reproduce original production source tests
`dotnet run -c Release --project Tools/B01/PARTSIZE001/P17Balance/BalanceTests.csproj`

GitHub Actions: `.github/workflows/b01-23-p17-balance-source.yml` Windows and Ubuntu builds run **the original .cs source** linked via csproj and 36+ tests including positive, negative, rotation and uncertain-input fail-closed cases. Consult Actions output for the **actual** number and PASS. Source is not compiled by Unity unless separately licensed/operator-approved. NO native source/physics PASS based on this suite.

## Install / Rollback / Codex prompt
Source-isolated assets: `Assets/PARTSIZE001/P17Balance/`; independent tests: `Tools/B01/PARTSIZE001/P17Balance/`. Codex must explicitly claim scope before any integration. In a **disposable** licensed Unity 6000.3.14f1 receiver branch, import ONLY this folder and .meta, compile, run exact positive/negative editor and play tests with measured actual masses + wheel/track/hover contacts. Independent B01-40 verifies GUID/asmdef/file scope; B01-60 checks source and real native. Never substitute logical graph or dummy contacts. Keep data proprietary to receiver and do not overwrite existing robot components.

Copy-ready Codex task after explicit B01-00 dispatch: "Inspect P17-MASSBALANCE R1 immutable source PR/SHA and dependency #405; if ACK permits, import only new P17Balance original .cs/.meta into a disposable Unity 6000.3.14f1 branch. Bind actual measured part mass/CoM and actual grounded supports, not logical grid substitutes. Prove native compile, EditMode/PlayMode, 4 yaw rotations, elevated COM, missing contacts/negative mass fail-closed, and real suspension/traction as separate behavioral tests. Report source/receiver SHA, XML, logs and rollback. Do not change frozen producer or Codex canonical Garage without specific ownership transfer."

Rollback = remove **only** staged `Assets/PARTSIZE001/P17Balance/` and its matching folder .meta in disposable receiver. Original parent PARTSIZE and all Codex game source remain untouched. Never merge main/develop from this PR. G0 producer source only when CI passes; independent G0 pending; G1 NATIVE_NOT_RUN; G2 HOLD; CODEX_ACK NO.
