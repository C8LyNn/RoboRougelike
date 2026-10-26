# ECON-REBUILD-001 QA MATRIX

## Executable .NET8 portable tests — source compiled under explicit Unity JsonUtility surrogate
25 named cases in `tests/Program.cs`; seeded 3000 transition case. Receipt `test-results.json` and exact SHA in the CI job. Not a licensed Unity engine test.

## Native Unity EditMode: authored but NOT_RUN
Three tests in `Assets/B01Packages/ECONREBUILD001/Tests/EditMode/RebuildQueueEditModeTests.cs`: free+paid queue + win, duplicate win, save/load. NUnit XML and Unity Editor log mandatory for actual G1.

## Required host/Windows G1 (not implemented / NOT_RUN)
- Start garage with original parts, 0HP only valid rebuilding state; compare repair >0HP route unaffected.
- First 0HP rebuild reservation free in genuine between-challenge epoch; second discounted quote strictly below purchase; insufficient scrap refuses without partial state change.
- Win exactly once completes whole batch atomically, does not reuse physical ID or regenerate legendary ID/level/upgrades; duplicate Win event, defeat, restart, garage UI reopening, seed change DO NOT progress.
- During `REBUILDING`, part cannot be fired, mounted, sold, transferred, dismantled, looted, or re-reserved. Assert UI reason.
- With pending queue 0HP and no deployable vehicle, test approved #154 softlock policy; do not auto-heal.
- Crash/reload after first free order, paid order, defeat, queued win and completed win, journal/event replay, partial host rollback, transaction failure and interrupted UI.
- Real player garage→mission victory→repair closeout→rebuild gameplay loop in Windows; distinct Light/Heavy/Standard hulls; measured durability economy and salvage option interplay.

## Source gate vs release
G0_SOURCE producer static+portable CI; G0_TARGET B01-40 separate live worktree and GUID; G1 Unity Editor/Windows and independent QA B01-60; G2 owner dispatch. Test outcomes never transfer across different exact source SHAs.
