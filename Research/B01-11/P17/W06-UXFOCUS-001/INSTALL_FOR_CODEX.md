# Optional future game adapter — reserved to Codex

## Reproducible source test
```sh
git fetch origin research/b01-11-p17-w06-uxfocus-001
git worktree add --detach /tmp/b01-11-uxfocus research/b01-11-p17-w06-uxfocus-001
dotnet run --project /tmp/b01-11-uxfocus/Research/B01-11/P17/W06-UXFOCUS-001/Tests/MenuNavTests.csproj -c Release
```
Create a disposable worktree. The current Codex worktree, its game files and shared CI must remain untouched.

## Consumer mapping (ONLY after B01-00 offer AND Codex explicit claim)
- Copy `Runtime/MenuNavCore.cs` to a Codex-owned game UI assembly if desired. No modifications to existing R1 HUD/garage/pause by this producer.
- Map **authoritative** `R1RunState.Ready` → `VehicleSelect`, `UpgradeChoice` → `RewardChoice`, and `Won/Lost` → `Results`. Host must supply actual number of offers `RewardCount`, selected vehicle index, real checkpoint availability, transitions and input-text editing state; no guessed fake save.
- Call `Step(snapshot, currentFocusId, command)` from real user keydown (Up/Down/Left/Right or pad D-pad). Map `NavAction` to actual available game actions **in Codex code only**. `Confirm` generates a semantic action only for a previously valid focus, never for a stale or unknown ID; initial focus acquisition never dispatches. `EditSeed` merely requests entering the host's existing text-edit field and host sets `IsTextEditing=true` until exit. Do not redirect Esc/Back/Start, which remain owned by game/pause.
- Render visible/high-contrast focus indication only on valid target rectangles; include screen reader/human testing and 720p/1080p scaling. Pure source code does not draw, install or compile under native Unity yet.
- Avoid repeated Confirm on held key by host keydown debouncing; this pure deterministic selector intentionally has no wall-clock time or global state. Validate shortcut keys 1/2/3 and Enter already owned by Codex; do not double-dispatch a frame.

## No unapproved gameplay mutation
Do not map `NavAction.ResumeCheckpoint` unless actual valid save; do not make a removed RewardChoice slot actionable. Host chooses and validates canonical calls, source is UI focus selection only.

## Rollback
Ignore/discard this isolated research branch and its dedicated workflow. If future Codex integration is authorized, Codex alone rolls back its copied file and adapter; no B01 producer edits to canonical code, scenes, prefabs, .meta or save files exist.
