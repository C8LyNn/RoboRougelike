# W6–W9 Integration Implementation Plan

> Use superpowers:executing-plans inline; independent read-only source audits may run in parallel. One fresh final review of completed integration changes. User explicitly requests autonomous execution without per-technical-step approval.

**Goal:** integrate every available original mechanic into playable Windows activities/modules, isolate missing sources, retain W5.
**Architecture:** R3/P12/R6 ownership remains authoritative; package-specific runtime adapters and separate road/race/defense activities.
**Tech stack:** Unity6000.3.14f1, existing packages, Windows x64 Mono, Python/PowerShell/Git.
**Spec:** `Docs/RRL_W6_W9_INTEGRATION_DESIGN.md` and user's W6–W9 brief.

## Global constraints
- Frozen W5 `2154b4341dd6b6fefb135187dc07b4b12d44f908`; no main/develop/W5 edits or merges.
- Source gate missing WPN001–006/DEF001–002/MOV011; never synthesize original implementations.
- Every mechanic has pinned source, ID, frozen feature branch, Draft PR, report; status centralized in `Docs/RRL_PACKAGE_INTEGRATION_STATUS.md`.
- Preserve GUIDs/scenes/prefabs, four-cell XYZ/contact ownership and existing HP/salvage/repair.
- Targeted RED→GREEN for new behavior; original-package tests; one full regression/build/native wave closure.

## Review focus
1. Real shell hits cover/objective once; nearby/scanned objects never take proxy damage.
2. Multi-collider/dead/detached parts do not gain duplicate HP, scan powers, loot or hazard damage.
3. Failed/cooldown/paused scans preserve original resource/snapshot semantics and exclude self.
4. Standalone activities return/restart without leaking enemies, physics, timers, inventory or campaign rewards.
5. New effects remain bounded and visual; W5 combat/projectiles/attachment behavior survives each wave.

## Task1 — source catalog and W6 gate
- [x] Verify W5 and all source PR SHAs; isolate checkout; search kit and verifier; mark exact missing sources.
- [ ] Commit source/design/status record; retain fresh W5 baseline610 cases without redundant initial rerun.

## Task2 — W7 COV001 + shared collider damage boundary
**Files:** imported COV001 source/tests/metas; new `Assets/PackageIntegration/RRLHitRouter.cs`, `RRLCoverAdapter.cs`; modify real shell collision in `Assets/Pack/P12_PipelineSafeTankController.cs` and arena construction; integration tests under `Assets/Tests/Editor/RRLPackageIntegrationTests.cs` and PlayMode equivalent.
**Interfaces:** `RRLHitRouter.Apply(Collider,float,Vector3,Vector3,Transform)` → existing P12 HP or explicit `IRRLHitReceiver.ReceiveHit(...)`; cover attaches to original arena obstacles only.
- [ ] Import pinned COV delta with no overwrite/GUID collision; add actual arena/shell tests and observe failure.
- [ ] Implement receiver/cover binding, verify target tests plus original COV tests, record source/integration SHA/report/feature checkpoint.

## Task3 — W7 HAZ001
**Files:** imported HAZ source/tests; `Assets/PackageIntegration/RRLHazardAdapter.cs`; arena hook and targeted tests.
**Interfaces:** original `DamageRequested(GameObject,float)` → shared damage boundary; original phase/cadence → visible warning ring and encounter lifecycle.
- [ ] RED: no active damage before telegraph; one target request resolves one attached P12 part, reset/transition stops damage.
- [ ] Integrate original cycle/indicator with safe arena placement; target/package tests and report/feature checkpoint.

## Task4 — W7 VFX001v5 and wave closure
**Files:** imported VFXv5 resources/runtime/tests, production event bridge, settled damage/shot event hooks, native fixture/report.
**Interfaces:** settled hit/destruction/muzzle/debris → original bounded VFX orchestrator; never call damage/forces.
- [ ] RED: real hit/death produces one classified visual; no duplicate legacy heuristic emission or physics changes.
- [ ] Integrate v5 only; package/target tests; full Edit/Play/build and real Windows COV/HAZ/VFX + W5 regression. Exclude any unverified mechanic from candidate.

## Task5 — W8 COM001 + SEN001
**Files:** pinned sources and adapters; new physical scanner device binding; existing R3 model/assembly/garage/palette/salvage; sensor HUD/input; targeted tests.
**Interfaces:** shared availability/activation snapshot boundary; distinct COM visible structure vs SEN area signals; source-defined settings/profiles.
- [ ] Audit overlap before code (done); RED physical mount/removal/loss/recovery/repair/map-copy and no self/occluded structural contacts.
- [ ] Integrate COM then SEN separately with checkpoints/reports/target package tests. MOV011 stays source-blocked unless kit arrives.
- [ ] Full wave regression/build/native Windows with both mounted/lost/repaired and old campaign flow.

## Task6 — W9 MAPR6 / RAC001 / OBJ001
**Files:** pinned sources; activity adapter/controller/HUD, mode selection, owned map roots; targeted tests and per-mechanic reports.
**Interfaces:** actual R3/R6 vehicle → original map construction/checkpoint challenge/reactor state; receiver boundary → actual reactor enemy hits; independent menu activities.
- [ ] RED actual road/junction drive/collisions and activity exit cleanup; original map tests; preserve Gauntlet arenas.
- [ ] RAC checkpoint order/elapsed/speed validation and single clock; real driving completion/restart/return.
- [ ] OBJ real AI attack reactor through one integrity pool; timed win/death/return/repair behavior.
- [ ] Full wave regression/build/native Windows transitions across old/new modes, final candidate ZIP/extraction/launch.

## Task7 — final review and Draft PRs
- [ ] One fresh final review, fix real Critical/Important issues in one verified pass; correct factual evidence errors.
- [ ] Finalize every package source/SHA/runtime-vs-imported/tests/blocks/dependencies/READY status.
- [ ] Publish each frozen feature checkpoint and scoped stacked Draft PR, plus aggregate Draft PR to W5; attach all created PRs. No shared-branch merges.
- [ ] Deliver verified Windows candidate/local path, exact SHA/hash/size and honest unresolved source/UIA/cloud activation limits.
