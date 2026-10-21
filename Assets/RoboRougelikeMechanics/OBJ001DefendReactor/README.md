# OBJ-001 — Reactor Defense (Unity 6000.3.14f1)

Standalone opt-in protect-object objective; no main-scene, enemy AI, spawn, rewards or damage-pipeline changes.

- Add `ReactorDefenseObjective` to an isolated GameObject and call `Begin()` explicitly.
- Configure HP and survival time with `Configure(maxHp, seconds)`; invalid values and active reconfiguration are rejected.
- External systems call `ApplyDamage(amount)` and `Repair(amount)`. Events report actual changes, not requested amounts. Repair clamps to maximum.
- `Tick(deltaSeconds)` advances the countdown; default automatic clock calls Tick from Update. `SetAutomaticClock(false)` allows deterministic external clocking.
- Fatal damage before the deadline = Lost; deadline reached first = Won. Terminal outcomes are immutable until `ResetObjective()`.
- C# events: `DamageTaken(float)`, `RepairApplied(float)`, `StateChanged(DefenseState)`. Inspector UnityEvents: started, damage, repair, won, lost, reset.
- Disabled components reject input and pause their automatic countdown.

## Opt-in demo
Unity menu **Tools > RoboRougelike > OBJ-001 > Open Isolated Reactor Demo**. The menu asks to save any modified scenes, then creates a new **unsaved** scene with a reactor cube and demo UI. Enter Play Mode and use Start / Attack / Repair / Reset. Never save it over `Assets/scene 1.unity`.

## Tests
Unity Test Runner: `RoboRougelike.OBJ001.EditModeTests` (11 cases), `RoboRougelike.OBJ001.PlayModeTests` (3 cases). Run via `Tools/CI/Run-Unity.ps1` on a dedicated worktree with Unity 6000.3.14f1. Tests have not been executed in this environment.

## Integration boundary
No real-enemy adapter yet. This code is not automatically active in the Gauntlet. An optional future adapter must be isolated inside OBJ-001 and gated by Unity QA and review against Codex PR #1.
