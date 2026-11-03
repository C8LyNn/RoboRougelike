# B01-13 — PASS 17 / WAVE 30 — Interactive Windows Player Session / R1

**Producer:** B01-13 HUMAN PLAYTESTING. **HQ:** https://github.com/C8LyNn/RoboRougelike/issues/800. **Owner source parent:** W29 [#869 @ 6affe78371a4c9c0bcb90835ebb9bffee6976da9](https://github.com/C8LyNn/RoboRougelike/pull/869), inheriting W28 #844 and W27 #819. **No changes to Codex playable #873/#877, Saw #861, Save #826 or protected branches.**

## Concrete functional increment

The previous W29 evidence intake hashes submitted ZIP and Player.log files, but does not **launch** a Windows executable. W30 adds a concrete opt-in Python 3.11+ process-launch operator. On a real Windows host, an operator can point it at an **already extracted** Unity 6000.3.14f1 Windows Player, verify the EXE's actual SHA-256 before execution, launch the exact EXE interactively with its own `-logFile` path, and collect the real OS process exit code, UTC start/end, physical Player.log SHA-256/size, and a non-overwriting JSON receipt. This is a real executable tool; it does not simulate any RoboRougelike gameplay, override the economy, spawn victories, or touch other agents' sources.

**Required player layout:** `RoboRougelike.exe` (or another actual Unity Player EXE), sibling `UnityPlayer.dll`, sibling `<EXE stem>_Data/boot.config` or `globalgamemanagers`. The tool checks presence and structural paths, not authenticity of the binaries, their signatures, or their Unity version. It does not extract/download/build a ZIP.

### Real operator procedure (Windows, explicit opt-in)

1. Use a **disposable Windows account** or otherwise back up the real game's persistent saves; launch can mutate Unity `persistentDataPath` and other Windows user state. Tool cannot guarantee Save isolation, despite the separate *log* folder.
2. Extract the selected authorized Windows Player ZIP *outside* any active repo/worktree, verify its delivery hash independently, and note its **real game commit SHA** (must match actual build records, not the #869 research commit). Ensure the directory does not contain personal sensitive data.
3. Create a new **empty** evidence output folder separate from game install, not under a Git worktree.
4. Run preliminary check with **no game process launched**:

```powershell
python Research/B01-13/P17W30/native_session.py --exe "D:\RRL-Player\RoboRougelike.exe" --output-dir "D:\RRL-QA\session01" --game-commit <ACTUAL_GAME_COMMIT_40HEX> --expected-exe-sha256 <ACTUAL_EXE_SHA256_64HEX> --check-only
```

5. Only after user/operator consent and save backup, launch **the interactive actual Player**, play the real UI/game personally, and close the window:

```powershell
python Research/B01-13/P17W30/native_session.py --exe "D:\RRL-Player\RoboRougelike.exe" --output-dir "D:\RRL-QA\session01" --game-commit <ACTUAL_GAME_COMMIT_40HEX> --expected-exe-sha256 <ACTUAL_EXE_SHA256_64HEX> --interactive --ack-save-risk
```

6. Preserve generated `W30_Player.log`, `W30_SESSION_RECEIPT.json`, the original selected player ZIP and SHA, game-owned Editor/NUnit evidence if separately run, and independently collected test screenshots/observations. Use existing B01-13 W27 [#819](https://github.com/C8LyNn/RoboRougelike/pull/819) eight real-player tasks and W29 [#869](https://github.com/C8LyNn/RoboRougelike/pull/869) 21-case intake to record actual issues. Never substitute a mocked test's fixture log for a game log.

### What is measured and what is NOT established

The receipt attests **operator-side source bytes/process invocation outcome only**. `processExitCode=0` and a nonempty `Player.log` do not prove that the game was playable, that a human genuinely participated, that Unity tests passed, that a particular game commit was embedded in the EXE, that a Save was isolated, or that gameplay regression gates were met. `gameCommit` is explicitly `OPERATOR_ASSERTED`; the executable is pinned by its independently calculated SHA256. The tool reports `actualGameplayPassed=NOT_VERIFIED`, `unityNativeG1=NOT_VERIFIED`, `humanSessionsIndependentlyVerified=0`, and `playerLaunchG1=NOT_INDEPENDENTLY_VERIFIED` even for an exit 0. The log must be independently reviewed by B01-60. No player binary or native Unity activation is available to this agent. **W30 native GAME_G1=NOT_RUN, real human sessions=0.**

### Execution safeguards and regression tests

- Explicit `--interactive --ack-save-risk`; no process launches on `--check-only`; no attempt outside Windows.
- Refuses wrong executable SHA, relative paths, missing UnityPlayer.dll, missing Player_Data, symlinked executable, path collision with game install, output under a Git worktree, and existing Player.log/receipt.
- Never overwrites original game EXE/DLL/data, user Save, existing session output or Codex worktree; no network/telemetry, no Unity editor automation, no zip extraction.
- Each process receipt includes exact game EXE SHA256/bytes, UTC timing, OS exit, Player.log SHA256/bytes if present; if startup failed/no log/exit nonzero, the runner does **not** claim success.
- 17 original standard-library unit tests verify positive and negative control paths on temporary **synthetic files** using a mocked process. These are explicitly NOT Windows Player or Unity gameplay tests. GitHub Actions should run source tests on Windows 2025 and Ubuntu 24.04 (Python 3.11/3.13) and verify source manifest. No CI test launches a real game.

### Current W30 receiver selection — reference only

- Codex [#873 @ d537b7310979b91df27a08b14d0f1c74d0889089](https://github.com/C8LyNn/RoboRougelike/pull/873): selected cumulative **source-only** EMP + five-zone garage + JumpPad in real R5; native final Windows Player not confirmed.
- Codex [#877 @ 20273c3580079dfb299c30c85a67e76f8d893e7f](https://github.com/C8LyNn/RoboRougelike/pull/877): real garage install rejection pre-Scrap, parent #873; native final G1 not confirmed.
- Saw [#861 @ 8321131aac3e175255a357cf5a30b7716ef6e8fd](https://github.com/C8LyNn/RoboRougelike/pull/861): selected regression fix; independent native acceptance pending.
- Save [#826 @ 99fb08619c78b3834bb4c0450499babf991f218a](https://github.com/C8LyNn/RoboRougelike/pull/826): API source compatibility, native full-game Save/Load not confirmed. Do not add another writer.
- [#834](https://github.com/C8LyNn/RoboRougelike/issues/834): **owner decision PENDING** for real #812 one-shot loaner softlock; no policy exception or anti-softlock PASS.

**Rollback:** discard only this W30 add-only `Research/B01-13/P17W30` folder and W30 dedicated workflow branch; W29 #869 and all game/player branch revisions remain untouched. Do not install/merge as a game system.

**QA:** producer local SOURCE 17/17 PASS; Windows CI cross-platform tests pending remote action at authoring, independent B01-40 G0 pending, B01-60 native Unity G1 NOT_RUN; no playable Windows ZIP produced here and no real human playtest session conducted.
