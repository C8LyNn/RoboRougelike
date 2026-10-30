# UXCOMFORT-001 R1 executable cross-OS acceptance

```sh
dotnet run --project Research/B01-11/P17/W14-UXCOMFORT-001/Tests/UXComfortTests.csproj --configuration Release --no-launch-profile
```

This .NET8 test project directly compiles the exact original `Runtime/VisualStimulusGate.cs` with `TreatWarningsAsErrors`. 98 distinct strongly typed executable checks; no mock, API shim, fake Unity or simulation-only pass.

[Actual source CI #41087229667 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/41087229667) source commit `67e88bcf9bba1a21ab1a115aab22042757f371fe`: Ubuntu **98/98 PASS**, Windows **98/98 PASS**. Final documentation/manifest SHA rerun is required and independently recorded.

Original coverage includes rolling 1000ms cap boundaries and exact 249/250ms minimum gap, budget exhaustion with deterministic static replacement, critical glyph preservation even with flashing off, source-kind semantic mapping for all 7 cases, mode switching without cooldown reset, standard/reduced/no-flashes settings, intensity caps and high-frequency 5000-event floods, never-shaking reduced mode, no flash budget consumed by static/shake cues, negative malformed input/sequence/clock/run, stale old-run isolation, run reset, and near `long.MaxValue` arithmetic. The source test checks *the returned render recommendations*, not frame photons or actual player hardware output.

Native acceptance not yet performed: Codex-owned adapter in legal Unity 6000.3.14f1 must test actual camera/VFX and static fallback rendering on screen under repeated explosions, damage and critical warnings; verify world- and UI-space overlays with HDR/URP/post-processing; check keyboard/controller, screen-reader and motion-reduction preference changes. User experience and medical photosensitivity cannot be inferred from a millisecond gate alone.


## W15 R2 — direct zero-intensity regression, RED→GREEN

- Source-only test-first RED commit `a06d5e0b59a9fb24c38de5b54ed63bef74717fd9` adds **18 executable** original C# cases to unchanged prior R1 source. [Actual Ubuntu and Windows RED run #41091279821](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091279821) = **102 PASS /14 FAIL per OS**. The 14 FAIL demonstrate no-op flash consuming quota, blocking actual effect via 250ms gap or window count, and zero shake being incorrectly scheduled; the other 4 extra tests were already correct on R1.
- Fix commit `2a2bd304454efc3cc3bb825e5617b669e29b59f5`: zero-strength effects yield nonflashing semantic `StaticIndicator`, `Intensity=0`, `ReplacedByStatic`; no quota/timestamp spend. Run ID, source sequence and time accounting still advance. [Actual original-source GREEN run #41091389393](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091389393) = **116 PASS / 0 FAIL on Ubuntu, 116 PASS /0 FAIL Windows**. 98 historic cases remained unchanged and passed.
- Final R2 exact package-head SHA must rerun all 116 original .NET8 compiled C# tests; do not substitute a mocked UI, screenshots or source inspection for execution.

Extra adversarial cases: 5000 zero flashes keep 0 budget, zero `CriticalFlash` produces static Critical glyph, zero collision/part-detach amplitudes produce no timed shaking, zero incoming in reduced mode does not spend its only slot, zero flashes do not obstruct a real flash at same timestamp, no-flashes mode remains static, invalid or repeated source sequences rejected, and a real flash followed by zero then real does not falsely consume or delay the second real stimulus. Source-only, G1 native Unity **NOT_RUN**.
