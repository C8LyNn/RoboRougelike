# UXHAPTIC-001/R1 — original compiled .NET8 source test plan

```sh
dotnet run --project Research/B01-11/P17/W16-UXHAPTIC-001/Tests/UXHapticTests.csproj -c Release --no-launch-profile
```

`Tests/UXHapticTests.csproj` directly compiles the authentic `Runtime/HapticGate.cs` with `TreatWarningsAsErrors=true`, **not mocked/simulated Unity input hardware**. The original console test suite executes **114 distinct named runtime checks**. Real [GREEN #41097067292](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097067292) on actual source commit `b9974591f5360831526e0364d42b0f7fc66b4956`: **114 PASS/0 FAIL each Ubuntu & Windows**.

Coverage: five haptic kinds, three-tier directionality, intensity clamping and side-channel ratios, 80/160/300ms pulse durations, priority preemption and equal/lower suppression, exactly 180ms cooldown boundaries, disable-while-active `StopAllMotors` boolean, mode toggle persistence, zero strength / custom zero caps, run reset/old-run ignore, validation before mutations, replay prevention, 1000 event flood, and `long.MaxValue` duration saturation. Critical input ALWAYS requests a separate visual warning when current-run valid, regardless of motor enabled/playable/busy. Stale earlier-run critical MUST NOT create false visual warning.

### Authentic test-first regression proof
- [Original baseline #41096654228](https://github.com/C8LyNn/RoboRougelike/actions/runs/41096654228): **111 PASS each OS**.
- [RED #41096911640](https://github.com/C8LyNn/RoboRougelike/actions/runs/41096911640) at `31f0ca7a6937fcc7c3a37d29f63058b48117e502`: original runtime unchanged, 3 added tests, **112 PASS/2 FAIL each OS**. Stale-run CriticalDamage incorrectly returned `RequireVisibleCriticalAlert=true`.
- [Actual FIXED GREEN #41097067292](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097067292) at `b9974591f5360831526e0364d42b0f7fc66b4956`: **114 PASS/0 FAIL both OS**. False old-run visual alert removed, legitimate current-run visual alert unchanged.
- Final immutable package SHA rerun separate mandatory.

### Not tested
Actual native Unity Editor, PlayMode, PhysX, Input System gamepad, XInput/Steam Input/Quest/Steam Deck motor pulses, real on-screen alert, hardware accessibility, human QA: **NOT_RUN**. No Unity assets, .meta, GUID, asmdefs or game adapter. No code can be called CODEX_READY without separate receiver.
