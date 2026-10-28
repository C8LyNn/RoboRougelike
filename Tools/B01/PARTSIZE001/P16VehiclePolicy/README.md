# B01-23 / PASS16 — vehicle recovery, avoidance & unarmed ram policy (pure C# prototype)

**Status:** ORIGINAL IMPLEMENTATION + executable cross-platform tests; **NOT Unity, NOT integrated, NOT OFFERED for Codex installation, NO G1/G2**.

## Scope and owners

- B01-23 owns this **Tools-only isolated vehicle physical-behaviour prototype**. It does not change any frozen PARTSIZE #286, its independently accepted native QA #298, CHL #285, or Codex's integrated R3/R6/weapon/camera/garage/AI source.
- Separate B01-24 handles **enemy AI**; this module does **not** replace or claim that logic. Codex remains the sole possible integrator and must expressly accept a scope before any integration adapter is authored.
- No Rigidbody acceleration, collision/PhysX, enemy target acquisition, weapon aim or actual damage is implemented. The output is a deterministic **decision recommendation** from supplied, externally observed snapshots. Unity values are **not** faked.
- The original [#276 code governance](https://github.com/C8LyNn/RoboRougelike/issues/276) restricts new native gameplay source streams. This is an isolated **research/Tools C# prototype**, not a third Unity package; integration or promotion requires B01-00/HQ/Codex explicit scoped approval.

## Original runnable C# deliverable

`VehicleSafetyPolicy.cs` processes one immutable snapshot of individual physical mounted modules (**one unique part ID even if it occupies multiple logical cells**) plus normalized requested driver controls and external sensor measurements. It emits `Mode`, `Override`, recommended `Throttle`/`Steering`, `HasFunctionalWeapon`, `RamAuthorized`, and a stable reason.

- One functional core required; lost core disables output.
- Wheels/Tracks: **at least one functional left AND one functional right**. Omni/Hover: a functional selected drive. A healthy unselected drive cannot silently replace a broken selected drive; that policy remains Codex/HQ.
- A lost selected drive emits **zero new commanded throttle and turn**, not a velocity reset; this does not magically stop a coasting Rigidbody.
- Duplicate/empty IDs, invalid part enums/sides, NaN/negative sensor readings, nonfinite speed, illegal pilot control and unsupported drive mode **fail closed**.
- Forward ally within 5 m overrides the forward accelerator with stop; a front obstacle within 3 m stops forward motion and selects a clearer lateral direction if one side has at least 1.5 m clearance; otherwise brake, never choose an ambiguous tied corridor.
- **Unarmed ramming eligibility only**: user explicitly requests ram, *no operational gun*, selected drive functional, target confirmed hostile, target 2–12 m ahead, speed >=2 m/s, throttle >0.25, ally/obstacle corridors clear. This emits **permission to consider a ram**, NOT a Rigidbody force, hit event, weapon bypass, auto-aim or actual ram success.
- Reverse throttle is not overridden by *front* obstacles/allies; rear sensors are not present, so actual reverse-collision safety must be implemented by the approved owner before use.

The metre thresholds are **prototype parameters, not approved game balance rules**; B01-10/HQ must validate them. Sensor classifications and hit validity must come from a physical authoritative caller; no client-provided boolean is trusted in actual game integrations without Codex review.

## Reproduce (no Unity)

Requires .NET SDK 8 on Windows or Linux:

```bash
dotnet build Tools/B01/PARTSIZE001/P16VehiclePolicy/P16VehiclePolicy.csproj -c Release --warnaserror
dotnet run --project Tools/B01/PARTSIZE001/P16VehiclePolicy/P16VehiclePolicy.csproj -c Release --no-build -- --selftest
```

The console selftest executes 46 **original C# policy calls**, checks expected cases (normal driving, destruction/recovery, GUID uniqueness, wheel/track/omni/hover support, ally priority, obstacle gap choice, safe ram, low speed, malformed sensors and identical repeated snapshots). Test code invokes the actual production-class method `VehicleSafetyPolicy.Evaluate`, not a separate rewritten algorithm.

The dedicated workflow `.github/workflows/b01-23-pass16-vehicle-policy.yml` compiles the *actual* C# file with warnings-as-errors and runs the selftest on **Windows 2025 and Ubuntu 24.04**. An invalid CLI argument must exit nonzero. No Unity Editor is launched or claimed.

## Optional Codex intake / limitations

1. Codex/B01-00 first confirm *ownership of vehicle driver behavioural fallback* and its current integrated source SHA. The present work is **research prototype**, not a CODEX_CLAIMED source patch. Freeze the exact Git SHA/version only after B01-40 and B01-60 independent source review.
2. For later optional integration, map real `R3MountedPart` operational status/physical IDs/signed X-side and selected `R3DriveKind` into snapshot values, and provide actual vetted physics sensors. Never call this policy instead of existing state validation/sockets/PhysX.
3. **Reject** automatic integration if one module's left/right assumptions disagree with the host, damage snapshot is stale, ally detection is absent, or game does not support ram at the selected stage. Prototype sensor widths and threshold values have no independent physics acceptance.
4. Native tests still required on actual Unity Player for 2/6/12+ parts, all drive types, destruction/coasting, colliding obstacles, real friendly avoidance and actual target ram, with original logs/exit/scene screenshot. Do not infer from C# console green.
5. Any future native source requires a distinct approved package revision, actual `.meta`/asmdefs, original Edit/Play scene and independent G0/G1. **None are in this prototype**. Do not merge to `main` or `develop`.

**Code owner:** B01-23; **canonical integration:** Codex; **independent QA:** B01-40/60 if authorized. **Native status:** `NOT_RUN`. **Package status:** `TOOLS_PROTOTYPE / ACK_PENDING / G1=NOT_RUN / G2=HOLD`.
