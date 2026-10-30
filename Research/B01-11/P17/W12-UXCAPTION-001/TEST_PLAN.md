# UXCAPTION-001/R1 — source test plan and exact evidence

**Executable original .NET8 C# test project:**
```sh
dotnet run --project Research/B01-11/P17/W12-UXCAPTION-001/Tests/UXCaptionTests.csproj -c Release --no-launch-profile
```
The project directly compiles original `Runtime/CaptionTrack.cs` with `TreatWarningsAsErrors=true`, reads actual UTF-8 PL/EN localization JSON copied into the test output, parses via `System.Text.Json`, checks caption and directional labels. No runtime stubs, source-only text inspection or invented execution.

**Actual GitHub Actions:** initial [source #41073393017](https://github.com/C8LyNn/RoboRougelike/actions/runs/41073393017) 115/115 Ubuntu and 115/115 Windows PASS; expanded [source + real localization #41073897570](https://github.com/C8LyNn/RoboRougelike/actions/runs/41073897570) **150/150 PASS each OS, 300 actual source assertions**. Exact final package-head must be rerun before frozen release.

**Coverage:**
- 27 bearing angle boundary fixtures across all 8 directions, including front/back wrap at -180/+180, invalid bearing rejections.
- 89 named original source assertions (plus 27 sector loops) covering type severity, TTL exact boundary, 450ms coalescing, immutable snapshots, 32-event normal flood versus persistent urgent, cap/visible counts, full urgent queue fail-closed, events dropped due to urgent saturation, strict run/sequence validation, invalid localization keys/source IDs, negative/backward clocks, near-`long.MaxValue` arithmetic and reset.
- 35 additional real data/locale assertions: PL and EN JSON parse, 7 keys per language, 8 direction labels per language, `{count}` formatting and combined live warning+direction mapping.
- Negative coverage rejects arbitrary text/newlines/Unicode in source IDs and untrusted caption key strings. Host must supply structured IDs and whitelisted localization keys, never user-visible raw source ID as a caption.

**NOT_RUN:** no Unity Editor/PlayMode/PhysX/Windows Player, real sound samples, 3D engine source bearing, localization font shaping, controller accessibility, auditory/human playtest or integrated source; native G1 remains separate and mandatory for release. First native QA scenario should trigger a real hostile left/right/rear warning, mix repeated shots with urgent destruction, verify readable PL/EN strings with proper text wrapping and screen-reader semantics, and confirm single-event replay suppression.
