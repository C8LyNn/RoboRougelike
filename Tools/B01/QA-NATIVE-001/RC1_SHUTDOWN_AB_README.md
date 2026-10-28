# B01-60 / W13 — RC1→RC2 GUI shutdown C0000005 comparative QA

**Executable QA-only tool:** `rc1_shutdown_ab.py`; **test suite:** `test_rc1_shutdown_ab.py`.
This extends existing [QA-NATIVE-001](https://github.com/C8LyNn/RoboRougelike/pull/245); no producer game code changes, Unity upgrade, active Codex checkout or merge. Test plan is a source-pin example, **not an execution receipt**.

## Why this is P0

Codex RC1 [QA #248](https://github.com/C8LyNn/RoboRougelike/pull/248) preserved UI-controlled shutdown **0xC0000005** in both ordinary baseline and experimental MAPGEN build. Later [RC2 #258](https://github.com/C8LyNn/RoboRougelike/pull/258) produced seven finite client-free exits at native0, **but human/OS-UI shutdown was NOT_RUN**, so causality and remedy are not proven. B01-60 cannot currently execute licensed native Unity/Windows; this script is a real executable evidence collector for an authorized **independent Windows tester**, and source/selftests are portable.

## Independent operator instructions, same host and matching UI input

Use **original immutable BASE**, **final** RC1 EXP under `Final/` (not the older pre-guard variant), and frozen RC2 archive. Copy archives into a dedicated independent `C:\QA\Inputs` folder. Verify their exact SHA256 from PR#248/#258. **Do not access Codex active worktrees**. Use actual interactive desktop Windows x64 with identical display, resolution, GPU/driver, permissions and UI shutdown method for all six attempts. Test operator must distinguish menu-unavailable or startup failure from successful menu Exit, and record which action occurred. Exit codes alone cannot establish the menu was shown.

Edit the three `archive` paths and, if needed, the `exe` member path in `rc1_shutdown.example.json`. The SHA256 and 40-digit producer source pins are immutable; do not replace them with PR report HEADs.

```powershell
py -3 Tools\B01\QA-NATIVE-001\rc1_shutdown_ab.py prepare `
  --plan C:\QA\rc1_shutdown.json --evidence C:\QA\Evidence\RC1-W13-run001

# Each command opens the REAL extracted game EXE visibly. Operate the game's
# UI, exit using the identical UI method, and record exact operator action.
# The next trial must NOT be started until the previous GUI process ends.
py -3 Tools\B01\QA-NATIVE-001\rc1_shutdown_ab.py run --evidence C:\QA\Evidence\RC1-W13-run001 `
  --index 0 --timeout 240 --operator-note "Opened menu; pressed Alt+F4 once"
# Repeat index 1,2,3,4,5 strictly as BASE EXP EXP BASE RC2 RC2.

py -3 Tools\B01\QA-NATIVE-001\rc1_shutdown_ab.py verify `
  --evidence C:\QA\Evidence\RC1-W13-run001 --out C:\QA\Evidence\RC1-W13-verdict.json
```

`prepare` first checks **actual full archive SHA256** and scans ZIP contents for path traversal, symlink, Windows case-collision, missing EXE and excessive uncompressed size; then extracts **only into newly created QA evidence root**. In `run` the tool verifies the extracted EXE SHA256 again before launching it as an independent Windows GUI process, saves raw Player.log, actual OS exit code as both signed and `0xXXXXXXXX`, evidence timestamps, elapsed time, pinned EXE digest and operator's free-form interaction note. Nonzero exit, `C0000005`, missing or tampered log, wrong signature, reused attempt, missing cases and timeouts cannot produce green certification. Killed-by-timeout is **BLOCKED**, not an observed native crash.

`verify` independently rechecks the receipts and writes six **PASS / FAIL / BLOCKED / NOT_RUN** case statuses. Its top-level `PASS` means *only* the six external-operator-supplied receipts describe six clean exits and have intact log/hash fields; it does not assert hands-on B01-60 native execution, UI correctness or full game acceptance. The collector labels output `EXTERNAL_OPERATOR_PROVIDED_NATIVE_EVIDENCE_NOT_B01_60_INDEPENDENT_RUN` unless B01-60 later independently controls/repeats execution. An observed RC2 `C0000005` is a regression blocker; a clean sampled RC2 is **not** proof of a source-code fix. Even equal BASE+EXP crashes do not prove the root cause or eliminate MAPGEN interactions.

**Expected retained artifacts:** original three ZIP SHA256, full extracted EXEs and hashes, plan/prepared records, six folders of native `Player.log` + immutable `receipt.json`, and one separately written final verdict. For deeper crash causality, manually attach Windows Event Viewer *Application Error 1000* / *Windows Error Reporting 1001* for the correct executable/time, and a scrubbed Unity crash log if available; do not upload unredacted local paths, machine identifiers or user data. If a game aborts before Player.log creation, record native exit and FAIL + NO_LOG, never impute PASS.

## Scope / rollback / required next action

No assets, production scenes or save data are altered by this tool. Rollback: close the QA-launched Player, archive forensic results, then delete **only** the dedicated `C:\QA\Evidence\...` folder after retaining receipts. Input release ZIPs are read-only and the active game is never edited. Existing QA suite/plan runs independently; this is **not** a replacement for Edit/Play NUnit, physical R6 tests or human gauntlet. New snapshots need new exact SHA/ZIP pins and a new evidence directory. B01-00 assigns any confirmed crash fix to the appropriate producer, B01-60 retests on a new SHA. **G2 HOLD / NO MERGE**.

## Real executed selftests vs native

Run `python -m unittest -v test_rc1_shutdown_ab.py` with stdlib Python 3.10+; this tests genuine ZIP extraction/hashes, adversarial cases and synthetic receipt verdict logic. **No Unity Windows Player or human UI was executed by B01-60 here (`NATIVE=NOT_RUN`).**
