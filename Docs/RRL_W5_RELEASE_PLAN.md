# W5 Release Candidate Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline. One fresh review of W5 changes and release evidence at the end; do not repeat the completed W3/W4 review.

**Goal:** Deliver a complete, extracted-and-launched `RoboRoguelike_W5_RC1_Windows_x64.zip` and a Draft release PR into develop.
**Architecture:** Preserve verified W2/W3/W4 code and history. Investigate shutdown with controlled comparisons, then fix only reproduced game defects. Keep diagnostic and input-pilot instrumentation confined to the existing QA build define. Package the retail build, not the QA executable.
**Tech stack:** Unity 6000.3.14f1, existing packages, Windows x64 Mono, Git, PowerShell/Python.
**Spec:** User's authorized W5 brief: release integration, all six stages and restart, crash classification, full Unity regression, performance observation, ZIP extraction/launch, QA report and Draft PR. No new mechanics/models; no automatic merges into develop/main.

## Global constraints
- Branch `release/rrl-w5-candidate` starts at develop `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.
- Merge W2 `a0f0a6590b07ed2f5007361a7e761f133e52ff99`, then PR19 `916bf6d195bb7647f159a4dc50a4d6b2ab18b063`, preserving ancestry. Do not separately import PR17/18.
- Verified W3/W4 game/test source is `5d4b61d1160e9cceac888e070147aead71dfb92b`; reuse its 434 EditMode/176 PlayMode baseline when the source tree matches. Run one final full regression after actual W5 changes.
- Do not edit Assets while this checkout's batch Editor runs. No engine/package upgrades, license workarounds, raw logs/dumps or binaries in Git.
- Clearly distinguish damage/API fixtures, input-pilot combat and operator UI actions. No six-stage PASS without six actual victories and final reward/restart evidence.

## Review focus
1. An archive that works only in its build directory: verify required DLL/data/Mono files and compare all extracted hashes before launching the extracted retail player.
2. Crash attribution by assumption: compare the same QA binary's Application.Quit with/without computer-use observation and a game-free scene; retain native exit codes and fault module/stack evidence.
3. Input-pilot navigation mistaken for game failure/success: record route, real shots/AI/debris and completed stages; do not bypass combat with damage/reward APIs.
4. Final reward or restart retains state: verify six acquisitions, Won UI, then fresh menu/vehicle/currency/bonuses after restart.
5. Performance averages hide stalls: record combat frame-time percentiles, enemy/part/debris peaks and transition duration, including sampling method and QA overhead.

## Task 1: Release history and source integrity
**Files:** Git history, this plan, `Docs/RRL_W5_RELEASE_QA.md`.
**Interfaces:** Input verified PR/develop SHAs; output isolated merge commit and baseline source equality.
- [x] Verify live PRs/develop, create isolated worktree and merge W2 then PR19 without importing PR17/18 again.
- [x] Validate tracked metas/scenes/packages and exact Assets/Packages/ProjectSettings equality against PR19; retain inherited test results as baseline, not new W5 runs.

## Task 2: Shutdown investigation and complete native run
**Files:** Existing `Assets/R4/RRLW2RuntimeQA.cs`; QA-only diagnostic utility if required; relevant production/test files only after a reproduced defect.
**Interfaces:** Existing command-line QA installation and public run APIs; output native-exit matrix, complete input-pilot run/restart, performance samples.
- [x] Gather existing crash logs and fault metadata; compare no-observer Application.Quit with observed shutdown. If needed, repeat with an empty scene or alternate renderer, one variable at a time.
- [x] Add only QA-defined diagnostics/performance sampling needed for a conclusive probe. A production fix must follow a reproduced failure and RED→GREEN regression.
- [x] Complete six stages using real input/projectiles/active AI, operator reward/garage actions, final reward/Won/restart. Use an unobstructed valid route before changing the simple QA pilot.
- [x] Verify 720p/1080p, mounted/lost/repaired bonuses, bridge detachment and part-state persistence using the existing fixtures plus new evidence where W5 changes matter. Record API steps separately.

## Task 3: Final regression and redistributable
**Files:** Existing Unity runner/build scripts; release packaging helper only if useful; local ignored release output.
**Interfaces:** Stable tested source → retail Windows files → ZIP → extracted matching files and launched retail process.
- [x] Run full EditMode, PlayMode and Windows builds once after fixes; read native codes/XML/logs. Run repository/CI validators and preserve configuration versions.
- [x] Create exact requested ZIP with all retail runtime dependencies and a source/checksum manifest. Exclude QA, logs, symbols not required at runtime and licensing data.
- [x] Extract to a fresh directory, compare every included file hash and launch the extracted retail EXE. Record startup, renderer, errors, close outcome and ZIP bytes/SHA256.

## Task 4: Review, report and publication
**Files:** `Docs/RRL_W5_RELEASE_QA.md`, curated evidence and plan status.
**Interfaces:** Verified local deliverables → one Draft PR from release branch into develop; accessible ZIP path/link.
- [ ] One fresh review of W5 delta, merge provenance, test/ZIP evidence and unresolved limits; fix Important/Critical issues in one regression-backed pass.
- [ ] Report exact source/final SHAs, counts, six-stage/native versus API results, performance, crash classification, archive bytes/location and READY/BLOCKED honestly.
- [ ] Commit/push release branch, create and attach Draft PR targeting develop, inspect exact remote SHA/checks; do not merge or alter source PRs. Provide local ZIP path if binary publication is unavailable.
