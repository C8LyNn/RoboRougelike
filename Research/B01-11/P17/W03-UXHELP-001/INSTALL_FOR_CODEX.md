# Receiver / optional Unity integration (NOT performed)

```sh
git fetch origin research/b01-11-p17-w03-uxhelp-001
git worktree add --detach /tmp/b0111-uxhelp research/b01-11-p17-w03-uxhelp-001
dotnet run --project /tmp/b0111-uxhelp/Research/B01-11/P17/W03-UXHELP-001/Tests/UXHelpTests.csproj -c Release
```

**Codex sole owner of canonical game integration**: After explicit scoped ACK only, copy `Runtime/UXHelpCore.cs` to a Codex-owned Unity assembly. Provide real R1RunState/R3DriveKind mapping; actual `HasSelectedDriveMobility` (DO NOT approximate from wheel count), `MountedWeapons` / `ActiveWeapons` from authoritative runtime, actual A/B count, true run checkpoint existence and core HP. Host UI chooses placement, calls `Select` and `Text("pl"|"en")`, displays ALL critical reasons regardless of optional hints and dismissals. Host remains authoritative for repair, inputs, combat and save behavior. This source changes none of those. Never copy test assembly into game.

Minimum future Unity tests (not executed): simultaneous drive/gun failure, tips OFF, owned 0HP weapon, selected Omni controls (W/S, A/D, Z/C), selected Wheels/Tracks controls (W/S, A/D), correct LPM/PPM group semantics, nonexistent save not shown, localization clipping, 720p and 1080p. No "native PASS" claim without actual Codex Player receipt.

**Rollback:** simply delete/ignore this isolated research branch. If later integrated, only Codex may revert its own adapter and copy; nothing was added to game files by this package.
