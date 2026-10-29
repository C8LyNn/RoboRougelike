# Executed source tests / honest boundaries

**Original C# on two systems**: [GitHub Actions #40908953821 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40908953821). CI runs `dotnet run --project Research/B01-11/P17/W03-UXHELP-001/Tests/UXHelpTests.csproj --configuration Release` against the exact original `Runtime/UXHelpCore.cs`, not a JS/Python reimplementation. **46/46 Ubuntu PASS + 46/46 Windows PASS = 92/92 executed assertions, 0 FAIL**.

Coverage: first combat Wheels/Tracks vs Omni/Hover hints, two independent weapon groups, stage gating, checkpoint false/true, missing/broken weapon, no active gun, two simultaneous critical garage blockers, optional tips OFF and dismissed critical, HP 0 vs <=25% exactly, invalid counts/stages/drive types, copy localization and severity, duplicate IDs, no spurious safe-state hints. `Tests/UXHelpTests.csproj` requires .NET8 and has no external package.

**NOT_RUN**: native Unity 6000.3.14f1 EditMode, PlayMode, Windows Player; human comprehension; actual player-screen placement; underlying game-system integration; independent B01-40/B01-60 QA. CI-02/03 requiring legitimate Unity activation are NOT evidence of a native run.
