# OBJ-001 Reactor Defense Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Ship a playable optional three-wave reactor defense using PR14 and current grounded physics.
**Architecture:** Original objective delegates integrity to canonical P12 HP when bound; an R1 partial adapter uses existing encounter AI, projectile, salvage, reward and garage flow. One body/HP/AI/economy authority.
**Tech Stack:** Unity6000.3.14f1, C#/URP, existing licensed local CI and Windows x64.
**Spec:** Docs/superpowers/specs/2026-10-25-obj001-reactor-design.md.

## Global Constraints
- Base916654634d1bfdc2bbc14e64b9eeac856ede1fa8/PR125, work feature/rrl-obj001-integration in its own checkout.
- Reuse PR14cdc36875efe4b5661a7c41c0b7b60d0010850e2b files and GUIDs; no MAPGEN99/103, main/develop merge, Unity/package upgrade or independent model integration.
- Existing host damage, enemies, R6, garage and rewards remain authoritative; no sandbox budget laundering or intermediate-wave rewards.
- Report actual results/methods and known shutdown limitation; Draft only.

## Review Focus
- Fatal reactor hit and final enemy clear in the same physics step must lose, without duplicate reward.
- Pause/intermission, dead targets and retargeting must not leak ghost fire or advance waves while paused.
- Imported original package timed/demo behavior must remain valid under external HP binding/reset/destruction.
- Wave transitions must preserve accumulated salvage and vehicle damage/cooldowns without duplicate enemy IDs.
- Restart/menu/ordinary fight after defense must have no stale objective, callbacks, shots or altered standard economy.

### Task1: Import and characterize original package / missing host entry
Files: existing PR14 OBJ001 folder/metas and Docs/ParallelMechanics/OBJ-001; new Assets/Tests/Editor/OBJ001IntegrationTests.cs.
- [ ] Import exact original files, source/hash/GUID audit; baseline Python16 and package11 Edit/3 Play; actual runner evidence.
- [ ] Add actual missing preparation/entry and explicit-completion contracts using existing real R1 scene; run RED before implementation.
- [ ] Commit source attribution/design/plan/RED tests.

### Task2: Bind canonical damage and original objective lifecycle
Files: package ReactorDefenseObjective.cs, new IReactorIntegritySource.cs, new Assets/OBJ001Integration/OBJ001ReactorActor.cs and metas; tests.
Interfaces: IReactorIntegritySource exposes CurrentIntegrity/MaximumIntegrity, Damage/Repair/Reset; ReactorDefenseObjective.BindIntegritySource(source), SynchronizeIntegrity(), CompleteDefense(); OBJ001ReactorActor.Build(parent,material,position,health) returns actor with Damageable and Objective.
- [ ] RED actual canonical HP/damage/repair/death and explicit live completion, original timed behavior preserved.
- [ ] Implement external authority/read-only projection and safe finite/reset/terminal handling; build static reactor using existing damage/feedback and simple existing primitives.
- [ ] GREEN original+new lifecycle and actual physical-hit cases; commit.

### Task3: Real host activity / waves / target policy / HUD
Files: new OBJ001RunController.cs and OBJ001RunHUD.cs partials in integration; small hooks R1RunController.cs, R1RunProgress.cs, R1RunHUD.cs; P12_PipelineSafeEnemyTurret.cs setter/fallback only; tests.
Interfaces: PrepareReactorRun(), ChooseReactorDefense(), ReactorActive/ReactorHp/DefenseWave/DefenseWaveCount; Progress.PrepareGarage(); P12.RetargetEncounter(Transform), EncounterTarget read-only; same existing Spawn/Clear/Salvage APIs.
- [ ] RED waves/reward-once, reactor/player defeat, real AI fire on both targets, garage entry/exit, pause, simultaneous last kill and reset cleanup.
- [ ] Implement600HP,2/2/3 waves and3s intermission; use one salvage ledger with unique wave prefixes, canonical stage reward once, existing victory/loss settlement.
- [ ] Expose menu/preparation and campaign garage activity; block sandbox, invalid/double calls and obsolete callbacks; HUD readable at720/1080.
- [ ] GREEN targeted regression including ordinary fight and range; commit.

### Task4: Native QA / whole-branch review / final regression / delivery
Files: QA-only OBJ001RuntimeQA.cs if necessary, Docs/OBJ001_INTEGRATION_QA.md and evidence; release package helper only if report label needs extension.
- [ ] One fresh whole-branch read-only reviewer per executing-plans, then one RED/GREEN fix pass for important findings; record rulings/minors.
- [ ] Full EditMode/PlayMode after final targeted GREEN, inspect raw XML/native exits; build both Windows variants, original package tests included.
- [ ] Native actual AI/projectiles, three-wave win, reactor loss, restart/cleanup and garage/reward/next standard state; operator/pilot distinct from API/human. Verify retail launch and full ZIP extraction/hashes; preserve old RCs.
- [ ] Report source/final SHA, exact original code reused, actual tests/errors/limits/instructions/build; push own branch, create and attach Draft PR to PR125 branch. Stop without shared merge.
