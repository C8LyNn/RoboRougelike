# B01-25 W14 — MAGNETLANE001 R1

A complete original optional challenge source package: 6m by 3m magnetic surface produces a real lateral Unity ForceMode.Force on an actual Rigidbody. Warning 0.6s, active field 2.4s, harmless recovery 1s. Polarity reverses on each four-second cycle. Field force peaks at 60N at center and tapers to zero at lane edges. At 60N the 1.5kg Light rig accelerates 40m/s2 while 12kg Heavy accelerates 5m/s2. Pause, outside field, dead host or safe bypass never receive magnetic force. Continuous time clipping averages the field contribution correctly at phase boundaries. Input telemetry checked before state mutation; dt must be 0-100ms.

This is not SWEEPORB, SINKPLATE, secure reattach or canonical game. A real standalone Unity6 MonoBehaviour creates actual BoxCollider floors, an actual CapsuleCollider+Rigidbody Light/Heavy and applies original CSharp9 ForceMode.Force while the field is active. A fully permanent collision-ground northern bypass exists at Z4, unaffected by the field. Scene Builder generates a NEW isolated additive scene under Assets/B0125MagnetLane001/Scenes, refuses overwrite and restores prior active scene. Five native PlayMode cases authored, no native execution claim.

Reproduce from repo checkout with .NET8, Python3 and git:
    P=Research/B01-25/P17/MAGNETLANE001
    mkdir -p "$P/.output"
    dotnet build "$P/MagnetLaneRuntime.csproj" -c Release -p:LangVersion=9.0 -warnaserror
    dotnet run --project "$P/MagnetLaneTests.csproj" -c Release -- "$P/.output/gameplay.json"
    python Tools/B01/B01-25/P17W14MagnetLane/check.py --out Tools/B01/B01-25/P17W14MagnetLane/.output/receipt.json

Unique GitHub Actions runs original compiled pure CSharp9 gameplay positive and malformed-input tests on Windows and Ubuntu, 18 real source SHA/GUID/asmdef/inventory/ZIP/rollback checks, then compares raw original 20-entry Unity-importable source ZIP byte-for-byte across OS. Do not claim green until final exact SHA CI proves it. No original Unity Test Runner XML, Editor.log or Player/PhysX execution exists: UNITY_G1_NOT_RUN.

Receivers: B01-40 verify source ZIP and 29 immutable per-path Git blob pins and rollback for G0; B01-60 independently test source then run real licensed Unity 6000.3.14f1 PlayMode in disposable project (5 authored cases, Light/Heavy and safe bypass), record XML, logs and exit. B01-00 alone authorizes any later Codex offer; Codex ACK before canonical integration. No merge or game source overwrite. Rollback removes ONLY new Research/B01-25/P17/MAGNETLANE001, Assets/B0125MagnetLane001 plus root meta, Tools/B01/B01-25/P17W14MagnetLane and unique workflow; never delete preexisting/foreign directories. Demo intentionally samples center XZ rather than full compound collider, and is not canonical game HP/garage/weapon integration.
