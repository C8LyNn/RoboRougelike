# Executable original-C# evidence — UXFEED-001/R1

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
