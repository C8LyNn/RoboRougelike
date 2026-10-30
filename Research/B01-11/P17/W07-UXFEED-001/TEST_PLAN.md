# Executable original-C# evidence — UXFEED-001/R2

`dotnet run --project Research/B01-11/P17/W07-UXFEED-001/Tests/TacticalFeedTests.csproj --configuration Release`.

**87 actual assertions each OS**, including:
- Six event kinds, severity mapping and invalid enum rejection.
- Burst core damage/salvage aggregation with consecutive same source, last-time sliding 750 ms, immutable sum/occurrences/first+last sequencing.
- Distinct critical detach/weapon events never coalesce, normal events never starve critical entries, bounded visible slots, critical-only filter.
- 3000/6500 ms TTL exact boundaries, expired history retained, chronological ring-like bounded history and returned detached snapshots.
- Stale/duplicate/future sequence and out-of-order clock rejection, new-run reset and old-run ignore; rejected inputs never mutate sequence.
- Invalid subjects/markup-like ids, quantity, capacities and extreme long timestamps; deterministic ordering.

**Actual original source CI** [#41037701035 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/41037701035): Ubuntu 87/87 PASS; Windows 87/87 PASS; **174/174 executed, 0 FAIL**. Original source code is compiled as linked C# in `Tests/TacticalFeedTests.csproj`. Follow-up final HEAD CI is separately checked after manifest commit.

**NOT_RUN:** Unity 6000.3.14f1 Editor tests, PlayMode, actual Player HUD, in-game visual render, real combat event hooks, user playtest or audio/screen-reader. No synthetic native claims.

## W08 original adversarial 8 test cases / final source green
Run exact same .NET8 original source harness; eight new cases: critical survives normal history overflow; critical-only filter survives flood; two critical alerts survive flood; latest two replace earlier; TTL still expires; fixed-cap chronological history; new-run reset; no spurious critical from normal-only flood. [RED #41045734715](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045734715): Ubuntu **91 PASS / 4 FAIL**, Windows **91 PASS / 4 FAIL** on R1 source with new tests. [GREEN #41045982978](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045982978) on actual R2 C# source: Ubuntu **95 PASS / 0 FAIL**, Windows **95 PASS / 0 FAIL**. Previous 87 checks retained unchanged; **190 source assertions** total. Native Unity remains NOT_RUN.

## W09 R3 — exact-ticket acknowledgment and regression
17 additional executable original source tests exercise: API presence, successful critical acknowledgment, idempotency, non-mutating chronological history, stale ticket for same subject, independent other alert, previous-run token, normal/expired tokens, alert after 32+ normal-event overflow, immutable monotonic sequence/clock, single-slot older ticket, no reappearance after `AdvanceTo`, `Visible()` detached-array object identity, null ticket and fabricated matching data. Final direct C#-typed tests run the **real** `AcknowledgeCritical` method. [RED #41053157595](https://github.com/C8LyNn/RoboRougelike/actions/runs/41053157595) 100 PASS /10 FAIL each OS using test-first reflection adapter against original R2; [GREEN #41053373321](https://github.com/C8LyNn/RoboRougelike/actions/runs/41053373321) **112/112 PASS each OS** on original source and direct C# invocation. See [W09_ACK_RED_GREEN.md](EVIDENCE/W09_ACK_RED_GREEN.md). Neither result is native Unity G1.
