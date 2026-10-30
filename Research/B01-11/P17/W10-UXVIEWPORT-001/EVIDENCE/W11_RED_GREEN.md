# PASS17 W11 — B01-11 UXVIEWPORT-001/R2 original source RED→GREEN

**Purpose:** Fix source-derived layout assumptions and unsafe inset sum, without touching Codex's Unity gameplay. Previous R1 producer commit frozen at `4e967a27a483d2aee13c365c22835e11b2af6467` (75 compiled .NET8 tests each OS).

## RED
- Test-only commit `dd781f1c65a20245adc115eb7a302c59063b499d` adds 15 test-first checks to unchanged original `HudLayoutCore.cs`, using reflection exclusively for the not-yet-present measured metrics API so old R1 compiles.
- [GitHub Actions #41069079972](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069079972) **Ubuntu 75 PASS /15 FAIL, Windows 75 PASS /15 FAIL**, confirmed in job logs.
- Cases cover missing host-measured content API; critical panel growth and following-panel relocation; threats/vitals change; excess optional-content omission; mandatory fail-closed, baseline and scale compatibility; and two **actual R1 defects** where `left+right` or `top+bottom` integer overflow bypassed `HudViewport` constructor bounds.

## GREEN
- Source fix `199ff5db94e64a5f5b38f6cd5cff0b910d2c62ba`: adds `HudContentMetrics` constructor (five validated pixel heights, max 16384px), typed overload `HudLayoutCore.Build(HudViewport,HudContentMetrics)` and direct 64-bit inset additions. Default R1 `Build(v)` behavior unchanged.
- [Intermediate GREEN #41069229004](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069229004): Ubuntu **90/90 PASS**, Windows **90/90 PASS**, real original compiled source using the RED reflection adapter.
- Typed source/regression commit `5e6e272a71d2c1a899e48ae803bd80383170cc1b`: replaces reflection with **direct compile-time typed API** and adds seven targeted boundary tests.
- [Real original typed-source GREEN #41069518743](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069518743): **97 PASS/0 FAIL Ubuntu, 97 PASS/0 FAIL Windows =194 executed checks**, old 75 intact. Final packaged SHA must be rerun independently.
- Test project `Tests/HudViewportTests.csproj` directly compiles the original `Runtime/HudLayoutCore.cs` (no mock, fixture-only core or runtime shim).

## Limits / receiving parties
Host must supply actual inner-content text height measured after localization, wrapping and font scale; this algorithm doesn't read fonts, render text or instantiate Unity GUI. Strongly typed source tests don't prove real Unity/EditMode/PlayMode/G1. External B01-40 G0 and B01-60 original source QA **PENDING**. B01-00 decides any #79 OFFER, Codex alone ACKs/ports, game integration **NOT_DONE**. Rollback is discarding only own W10 research scope and CI workflow, or using earlier frozen R1 if required. No main/develop merge.
