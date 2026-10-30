# B01-60 / PASS17 W08 — DEBRIS-KINEMATICS-INDEPENDENT-QA-001

**Frozen producer:** B01-23 [#421](https://github.com/C8LyNn/RoboRougelike/pull/421) at `cb2b3a76e84266e2730fffc4ab048c1039f0652f`, original real Unity-compatible C#9 source Git blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`.

**Distinct coverage:** Original #421 producer verifies 33 authored cases and physical packaging. Independent B01-61 [#443](https://github.com/C8LyNn/RoboRougelike/pull/443) verifies the unchanged `DebrisRepulsionEmitter` adapter with a synthetic UnityEngine stub (47 cases + 5 live source mutants). This receiver instead **executes original `DebrisImpulseCore.cs` directly on 65,536 seeded three-dimensional detach configurations** and checks the physical impulse equation `J = mass * clamped delta-v`, change along the outward vector, zero induced tangential delta-v, per-axis correctness, deterministic ledger behavior, coincident-point fallback, 1024 translation and mirror symmetry cases each, 2048 mass linearity cases, invalid input atomicity and extreme valid constraints.

*Real function with existing compiled C# types; not a Python reimplementation, stubbed game mechanic or invented Unity-native result.* Per-sample deterministic receipt stores 128 representative quantized force vectors with SHA256 in CI output.

## Reproduce
Checkout PR QA branch (stacked on frozen producer without modifying it) from repository root with Python3 and .NET8:

```bash
python Tools/B01/B01-60/P17W08/DebrisKinematicsQA/source_gate.py
dotnet build Tools/B01/B01-23/P17W06DebrisRepulse001/DebrisRepulseTests.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-23/P17W06DebrisRepulse001/DebrisRepulseTests.csproj
dotnet build Tools/B01/B01-60/P17W08/DebrisKinematicsQA/DebrisKinematicsQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-60/P17W08/DebrisKinematicsQA/DebrisKinematicsQA.csproj
```

`source_gate.py` checks 8 exact original Git blobs including real core, adapter and asmdef, all five exact original Unity GUIDs, 8 negative tamper/scope fixtures, direct source parent SHA and all six QA-only changed paths. Independent Windows2025/Ubuntu24.04 GitHub Actions preserve raw producer+receiver logs even on RED.

## Gates and limitations
- **G0 source:** PASS only if the actual exact-head CI completes green on both systems.
- **G1 licensed Unity / actual Rigidbody PhysX:** **NOT_RUN**. Testing the mathematical impulse does NOT establish detachment callback timing, actual collider clearance, body movement, reattach state or game integration. Author's optional real two-rig playground [#439](https://github.com/C8LyNn/RoboRougelike/pull/439) provides seven real Unity PlayMode tests **authored but not executed**.
- **B01-40** formal physical asset/GUID/manifest intake independent, **B01-00** controls #79 Codex route; Codex ACK not observed, G2 HOLD. No merge.

## Exclusive ownership & rollback
Changes ONLY in `Tools/B01/B01-60/P17W08/DebrisKinematicsQA/*` and own `.github/workflows/b01-60-p17w08-debris-kinematics.yml`. No original `Assets/**`, producer #421/#439, CHL/PARTSIZE/MODEL_JOB, Codex R3/R6 or main/develop modifications. To rollback: close Draft QA PR / delete only this branch and six QA-only added paths. Original producer stays byte-for-byte identical.
