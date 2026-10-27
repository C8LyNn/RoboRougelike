# EXIT273 — normal exit, pause and shutdown QA

Status: **QA IN PROGRESS**. Native full suites and operator GUI results are not yet final. This document will be updated before delivery.

## Exact source and preservation

- Parent PR #272: `9cd5a5272a9ebda130a97a5f1f73ce3eaf24b976`; its delivered game code is `88b8eed60345c49481648833e1fde6dc1a400258`.
- Isolated branch: `fix/issue-273-exit-shutdown`, worktree `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-Exit273`.
- Current frozen Windows/native source: `8a294e6638d4a82619089c0ab5ad02110847c233`.
- Unity `6000.3.14f1 (d68c3f99a318)`, existing URP, Windows x64 Mono, Direct3D11 for the recorded Player trials. Packages, scenes and engine version unchanged.
- Existing #272 worktree remains clean at its original HEAD. No main/develop merge.
- [Integrity receipt](final-integrity.json): 976 unique GUIDs, no missing metas, duplicate GUIDs, deleted original Assets or changed original GUIDs; shared `Assets/B01Packages.meta` preserved byte-for-byte. Existing BUILD/CAMERA/RAC receiver deviations are the inherited #272 changes, not newly imported packages.
- Full integration coverage remains defined by [the original package matrix](../FullMerge270/CODEX_PACKAGE_MERGE_MATRIX.md). Prototype models remain owner-excluded.

## Behavior

The main menu has a visible **Wyjdź z gry** button. Esc opens session pause in combat, garage, activity selection and rewards. Pause offers resume, return to main menu and complete exit. The range retains its specialized training/reset/garage controls and additionally provides menu/exit.

Garage resume keeps `Time.timeScale == 0`; combat/range resume restores simulation and existing input. The overlay blocks garage pointer input, including the event that resumes the session. Keyboard arrows/Enter and gamepad Start/d-pad/south/east navigate the pause actions. Physical gamepad hardware validation is NOT_RUN.

Return uses the existing scene restart/cleanup, with an idempotent guard and cancellation of the old controller's coroutines. Quit is idempotent, releases input/cursor and restores the global clock. It uses `Application.Quit`, with Editor-safe stop-play behavior. Cleanup cannot prevent the actual quit call: it is in a finally block. There is no quit veto, confirmation modal, task wait, subprocess wait or normal process kill. Alt+F4 is also recognized before state/transition guards; native Windows close remains unblocked.

## Native execution so far

| Source/stage | Actual result | Evidence |
| --- | --- | --- |
| Baseline test source `d9398c3` inside restricted sandbox | INFRA_FAIL, editor exit -536870911 / 0xE0000001 before compilation; Mono socket/BuildReportRestService initialization stack | `FM270-EXIT273-Red-20261027T072955Z` |
| Same source, host Unity compile | PASS, native exit 0 | `FM270-EXIT273-RedHost-20261027T073132Z` |
| Baseline native EditMode red | FAIL: 1/2 passed, garage pause assertion failed | same stage, actual XML |
| First fix `d9af405` compile and Windows build | PASS, both native exit 0 | `FM270-EXIT273-Green-20261027T074013Z` |
| First fix targeted EditMode | PASS 11/11, native exit 0 | same stage, actual XML |
| Targeted PlayMode filter | NOT_RUN: 0 matching tests, not counted as PASS | same stage |
| Frozen `2e4bff3` compile and Windows build | PASS, both native exit 0 | `FM270-EXIT273-FinalFull-20261027T074441Z` |
| Earlier full EditMode at `2e4bff3` | ABORTED_NOT_A_TEST_RESULT, native -1; controlled cancellation on discovery of the R regression | same stage, intentional-abort.json |
| Native R-key reproduction `7b5d201` | FAIL 0/1, actual R observed but paused range did not reset | `FM270-EXIT273-RangeRed-20261027T080646Z` |
| Final `8a294e66` compile/build and focused EditMode | PASS: compile 0, build 0, 4/4 tests, native exit 0 | `FM270-EXIT273-RangeGreen-20261027T080853Z` |
| Final full EditMode / PlayMode `8a294e66` | RUNNING, no final result asserted | `FM270-EXIT273-ReleaseFull-20261027T081029Z` |

The red failure was a real behavioral assertion, not a compiler failure. Targeted cases exercise garage pause/resume, combat pause/menu cleanup, idempotent shutdown preparation, and the eight pre-existing full-activity regression cases. XML, phase exit receipts and scrubbed compressed logs are collected under `Evidence/` at completion.

## Windows Player evidence so far

Earlier candidate `2e4bff3`: twelve completed visible Windows Player processes, all actual native exit **0**. No UI automation client was attached. API-controlled scenarios below are not human GUI tests or OS Alt+F4/X proof.

| Actual scenario | Result | Details |
| --- | --- | --- |
| RequestQuit from main menu / 720p | PASS | New quit handler, actual process exit 0 |
| RequestQuit from paused garage / 720p | PASS | Pause rendered, actual exit 0 |
| RequestQuit from generated combat / 720p | PASS | Real enemies and generated world |
| RequestQuit while transitioning / 720p | PASS | `transitioning=true`, stopped during encounter setup |
| Generated combat → pause → menu → quit / 720p | PASS | New run Ready, old generated world gone |
| Extracted ZIP menu / 1080p | PASS | Actual extracted executable, exit 0 |
| Existing queued Input System / 720p and 1080p | PASS, 2 processes | Enter/garage, W/D, LMB physical shots, RMB behavior, Esc pause/resume |
| Extracted ZIP generated-map regression / 1080p | PASS | Generation, damage, rewards, repair, inventory/mount and failure/restart fixture |
| Extracted ZIP reactor regression / 1080p | PASS | Waves, real damage, victory/loss/restart fixture |
| Heavy six-stage integration loop / 1080p | PASS | 501.893 m force-driven R3/R6 travel, 12 wheel contacts maximum, 6 results/rewards, garage/repair/restart; 11 hostile projectiles observed |
| Extracted ZIP range / 1080p, 186 s | PASS | Live range simulation for 185 s, pause and new quit handler, actual exit 0 |

Combat settlement in the full loop is explicitly API-controlled after observing live enemies/projectiles. It is not claimed to be unassisted combat or a human full-playthrough. Camera/aim, physical modules, all road/race modes, OBJ and canonical progression remain covered by the inherited fixtures and the native suites; there is no new package import.

### Final extracted Windows build `8a294e66`

Nine automated processes completed, all native exit 0: menu, paused garage, generated combat, mid-transition, activity/menu return, range R reset, queued Input at 720p and 1080p, and the full Heavy six-stage loop. Every process ran the EXE extracted from the final ZIP and matched the built Assembly-CSharp SHA-256. The final loop travelled 501.873 m with up to 12 wheel contacts, observed 11 hostile projectiles and settled six canonical results before restart. API-controlled outcomes remain clearly labeled. R reset was tested by a queued native Input System event and rebuilt the range copy.

Both final owner GUI trials PASS: explicit main-menu quit at 720p and explicit pause quit at 1080p. Both final native process receipts are 0 / 0x00000000, source 8a294e66. The owner confirmed both buttons closed the game. These were plain retail launches with no diagnostic installer or UI automation attached. See operator-confirmation-final.json and the two FinalMenuButton/FinalPauseButton receipts. Final Windows total is 11/11 clean exits (9 automated, 2 manual); the earlier A/B remains attributed to its actual earlier binary.

The [independent read-only review](code-review.json) found no actionable code issue in the complete change. It did not run tests or assign native/full-suite results. The full native test run remains the authority for suite counts.

## Historical crash and GUI verification

Read-only Windows Application Error events again confirm historical `UnityPlayer.dll`, code `0xC0000005`, RVA `0xb1b031`. Existing matching-PDB evidence in [shutdown-symbols.json](../Evidence/W5/shutdown-symbols.json) places this address in the nearest public symbol `RuntimeStatic<PlatformAccessibilityManager,0>::StaticDestroy + 0x51`. The earlier minidump also lists `UIAutomationCore.DLL`; that is correlation and symbol attribution, not a proven private engine root cause.

Unity's [official 6000.3.21f1 release notes](https://unity.com/releases/editor/whats-new/6000.3.21f1) explicitly list UUM-146676: a Windows Player shutdown crash after UI Automation interaction. This is a plausible match to the historical evidence. The required `6000.3.14f1` is retained; this change does not claim to patch Unity's native accessibility code. No accessibility service or system setting was disabled.

Both supported computer-use entry points failed before attachment with `failed to write kernel assets` / OS error 3. Thus automated real mouse/X/Alt+F4 reproduction through that helper is NOT_RUN. The earlier owner confirmation that #272 garage and Alt+F4 worked is retained as a human report; no native exit receipt was captured for that earlier confirmation.

The exact B01-60 #245 recorder at `aa5eedf2c1406b04ad9a496eed82242521d1325a` is staged unchanged under `Tools/Exit273/Staged` with SHA-256 provenance. It validated/extracted three distinct pinned ZIPs and prepared its balanced schedule. BASE now explicitly maps to #272, EXP to this fix, RC2 to the historical #258 artifact; no labels imply unchanged original RC1 semantics. Requested human trials: BASE Alt+F4, EXP Alt+F4, EXP X, BASE X, followed by two new-build quit-button trials. All four BASE/EXP trials completed with actual native exit 0; the owner independently confirmed the requested Alt+F4/X actions. Both EXP logs reached the ordinary NativeClose callback. The two subsequent first-candidate manual processes also exited 0: 720p closure confirmed without separately identifying the button; 1080p garage -> activity -> Esc -> main menu -> exit confirmed. See operator-confirmation.json. The two historical RC2 comparison trials are optional and NOT_RUN unless executed. Do not relabel the complete six-run recorder PASS when only a subset runs.

**Fixed regression:** the pause menu initially intercepted the range R shortcut. A real native key test reproduced the failure; the corrected routing then passed the unchanged assertion, and final Windows RangeReset recorded rangeReset=true with exit 0. Historical UIA crash fix and hardware gamepad validation remain unproven.

## Archive and manual instructions

Current frozen ZIP: `C:\Users\kubap\Desktop\Prototypy\Releases\ROBOROUGELIKE_EXIT273_FINAL_20261027\RoboRougelike_EXIT273_Windows_x64.zip`, 38,590,408 bytes, SHA-256 `5cad21b49974abb588587121393180e2f74b72aff43f5ee61ac0a200d20941b9`.

EXE: `C:\Users\kubap\Desktop\Prototypy\Releases\ROBOROUGELIKE_EXIT273_FINAL_20261027\RoboRougelike_EXIT273_Windows_x64-VerifiedExtract\RoboRougelike.exe`. ZIP CRC and every extracted runtime file hash verified; the extracted game actually ran. The complete EXE, Unity data, UnityPlayer, Mono and required files are included; DoNotShip debug output excluded. `START_HERE.txt` is inside the ZIP.

Manual confirmation: quit from the main menu; relaunch, prepare a vehicle, start an activity, Esc/resume, Esc/menu, then restart an activity and quit from pause. Separately check Alt+F4 and X with the game focused, and relaunch after each exit. Retain Player.log and the process receipt. Return to menu ends the current run; it does not introduce a save-game system.

Rollback: close the new Player and launch the preserved #272 archive/extracted game. The old integration worktree and release bytes have not been overwritten.
