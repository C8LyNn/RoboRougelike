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


## W17 / R2 — run-boundary motor-stop contract

Re-run full original C# executable (same project, original runtime directly linked):
```sh
dotnet run --project Research/B01-11/P17/W16-UXHAPTIC-001/Tests/UXHapticTests.csproj -c Release --no-launch-profile
```

Twelve additional executable cases explicitly verify `StartNewRun(int)` returns boolean mandatory physical STOP for active old-run Minor/Major/Critical impulses; returns false for idle/expired/disabled/zero strength and a second immediate reset; still clears old run and allows fresh first source sequence; invalid new-run ID leaves existing active motor state intact; `SetEnabled(false)` continues to emit STOP on active motor. Test-first RED source is unchanged R1 at `eacbbb35a440edb83405919ffd34a7f11b3fc9bb`; actual C# repair `fc9046c022d1a0512b2858f43bc82cee41fef11a`. The full original suite now has **126 named executable checks per OS**. Run receipts: [RED #41100770919](https://github.com/C8LyNn/RoboRougelike/actions/runs/41100770919), [GREEN #41100849650](https://github.com/C8LyNn/RoboRougelike/actions/runs/41100849650); check actual result status and exact final SHA before release. New returned `bool` must be consumed by Codex-owned eventual driver; no native device evidence here.


## W18 R3 pause/resume regression test plan — 144 direct original C# cases/OS

```sh
dotnet run --project Research/B01-11/P17/W16-UXHAPTIC-001/Tests/UXHapticTests.csproj -c Release --no-launch-profile
```

This project **includes the actual original production** `Runtime/HapticGate.cs`, not a simulated C# model. Previous R2 **126 PASS/OS** verified at frozen exact SHA `fa6defe701818e79fec447b95fc94efc1cc76cd1`, [Actions #41101261126](https://github.com/C8LyNn/RoboRougelike/actions/runs/41101261126).

18 W18 additional original executable cases cover: `SetPaused(bool)` method signature, initial `Paused=false`, STOP obligation when entering pause mid-Minor and mid-Critical pulse, no stop for idle, no repeated STOP, resume no STOP, persistent `Enabled`, typed `SuppressedPaused`, critical visible alert when motors blocked, consumed replay sequence, prior `LastPlayedMs` cooldown across pause/resume, no output despite critical events during pause, next new run clears pause, old-run critical remains ignored, and invalid input fails while paused. Expected **144/144 per OS**. [RED #41104581405](https://github.com/C8LyNn/RoboRougelike/actions/runs/41104581405) at test-only SHA `604443a9c42557633d41519d50c13a5815553d68`; [GREEN #41104661493](https://github.com/C8LyNn/RoboRougelike/actions/runs/41104661493) at source implementation `3875a32a9c1fac5feba98d06ecede99196de926f`. Record actual logs and separately rerun final package HEAD; pending GitHub queue ≠ PASS.

Native Unity EditMode/PlayMode/gamepad/UX pause integration remains **NOT_RUN**; no physical motor-stop signal has actually been issued. Host must still obey STOP on pause, reset, disable and disconnection.
