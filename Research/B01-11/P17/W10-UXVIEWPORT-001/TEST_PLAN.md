# UXVIEWPORT-001/R1 — actual original C# test results and negative cases

## Reproduction
```sh
dotnet run --project Research/B01-11/P17/W10-UXVIEWPORT-001/Tests/HudViewportTests.csproj --configuration Release --no-launch-profile
```
The test project **links the very same `Runtime/HudLayoutCore.cs` original source**, compiled via .NET8 with compiler warnings treated as errors, rather than testing a mock, JSON response or JavaScript pseudocode. [GitHub Actions initial run #41065292153 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/41065292153), **Ubuntu 75/75 PASS; Windows 75/75 PASS**, 0 FAIL each. Final package-head CI must also be checked separately.

## Coverage
Eight explicit screen sizes: 320×568, 360×640, 768×1024 (portrait), 1024×768 (4:3), 1280×720, 1366×768, 1920×1080, 3840×2160 (4K); each combined with 100%, 125%, 150% user-scale for 24 geometry checks. Plus dedicated assertions for mandatory critical/pause and priorities, double-column vs portrait stack, optional objectives/tips, valid asymmetric safe-area and TV overscan insets, no intersections or clipping, detachment of returned panel lists, text-size growth, deterministic repeatability, invalid viewport/negative inset/out-of-range font scale/insufficient safe-area, and fail-closed for mandatory panels on constrained displays.

**NO** hardware-rendered Unity GUI, actual font asset legibility, screen reader, gamepad focus, PC/TV overscan integration or game player visual checks were executed. Those require an authorized owner/Codex integration and native QA. A real human should verify legibility at 768×1024, 1024×768 and 1366×768 prior to game release.

## W11 R2 — actual test-first regression and manual import acceptance
- R1 source remains immutable at `4e967a27a483d2aee13c365c22835e11b2af6467`.
- [RED #41069079972](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069079972): unchanged R1 runtime source plus 15 test-first assertions at `dd781f1c65a20245adc115eb7a302c59063b499d`. Ubuntu **75 PASS/15 FAIL**, Windows **75 PASS/15 FAIL**: no host-measured content overload (13 cases) and overflowing left/right / top/bottom constructor sums not rejected (2 cases).
- [GREEN #41069229004](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069229004): original R2 source, test-first adapter, **90/90 PASS** each OS.
- [Typed test GREEN #41069518743](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069518743): **97/97 PASS Ubuntu and Windows**, compiled direct `HudLayoutCore.Build(v,new HudContentMetrics(...))`, old 75 retained. 7 extra negative/compatibility cases: zero/null metrics, negative critical/optional, oversize measurements, double-overflow margins.
- Exact FINAL package SHA should independently rerun the same 97 source checks. Native Unity text metrics, `GUI.matrix`, font wrapping, actual visual layout on monitor, player pause/critical overlay, controller/gamepad and human UX **NOT_RUN**. For native G1, Codex must provide true text measurement and test changing language, scaled text, portraits, 4:3 and 4K at runtime.
