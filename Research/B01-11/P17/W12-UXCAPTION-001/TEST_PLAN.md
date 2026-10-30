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


## W13 / R2 critical-warning source regression (exact red/green)
- **Test-first RED:** at `c27aef4d9e0362a8cfe18a6c169a207600ea8b6e`, original R1 production source unchanged, 12 newly executable tests. [RED #41081844186](https://github.com/C8LyNn/RoboRougelike/actions/runs/41081844186): Ubuntu **153 PASS/9 FAIL** and Windows **153 PASS/9 FAIL**. 9 failures are real critical eviction/visibility defects; 3 added tests pass on old source.
- **Safety fix:** `39e1e921d63e5e092652d5efc01944a0cd361318`; initial GREEN run had **166 PASS/1 FAIL**, the remaining failed assertion assumed newer PartDetached precedes CriticalWarning. It was an obsolete test ordering expectation, not an unhandled safety defect. Updated original test expectation at `7c0c08a2cc84635772379d6a9254a853d7fac90a`.
- **Source tests now:** original 150 R1 +12 W13 adversarial +5 new typed priority/return/outcome/TTL assertions = **167 assertions per platform** (Windows/Ubuntu). The final packaged-head real CI result is authoritative; do not infer native Unity G1.
- New cases include 32+ part-event bursts, critical warning retention even at capacity=1, critical+part and two distinct critical alerts, smaller visible slots, priority ordering, normal/urgent/critical saturation, urgent-only projection, replay rejection after a dropped lower-priority event, expired critical release, critical cue coalescing, R1 outcome compatibility, 6000ms TTL.
- Old original 150 cases for angles, cap, monotonicity, partial telemetry, per-run resets and Polish/English JSON remain executable.

The output is deterministic **text/data snapshots only**, not a visual animation, speaker output, voice alert or a Unity localization renderer. Native Unity EditMode/PlayMode, real sound source instrumentation and on-screen human readability remain **NOT_RUN**.
