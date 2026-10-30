# UXCOMFORT-001 R1 executable cross-OS acceptance

```sh
dotnet run --project Research/B01-11/P17/W14-UXCOMFORT-001/Tests/UXComfortTests.csproj --configuration Release --no-launch-profile
```

This .NET8 test project directly compiles the exact original `Runtime/VisualStimulusGate.cs` with `TreatWarningsAsErrors`. 98 distinct strongly typed executable checks; no mock, API shim, fake Unity or simulation-only pass.

[Actual source CI #41087229667 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/41087229667) source commit `67e88bcf9bba1a21ab1a115aab22042757f371fe`: Ubuntu **98/98 PASS**, Windows **98/98 PASS**. Final documentation/manifest SHA rerun is required and independently recorded.

Original coverage includes rolling 1000ms cap boundaries and exact 249/250ms minimum gap, budget exhaustion with deterministic static replacement, critical glyph preservation even with flashing off, source-kind semantic mapping for all 7 cases, mode switching without cooldown reset, standard/reduced/no-flashes settings, intensity caps and high-frequency 5000-event floods, never-shaking reduced mode, no flash budget consumed by static/shake cues, negative malformed input/sequence/clock/run, stale old-run isolation, run reset, and near `long.MaxValue` arithmetic. The source test checks *the returned render recommendations*, not frame photons or actual player hardware output.

Native acceptance not yet performed: Codex-owned adapter in legal Unity 6000.3.14f1 must test actual camera/VFX and static fallback rendering on screen under repeated explosions, damage and critical warnings; verify world- and UI-space overlays with HDR/URP/post-processing; check keyboard/controller, screen-reader and motion-reduction preference changes. User experience and medical photosensitivity cannot be inferred from a millisecond gate alone.
