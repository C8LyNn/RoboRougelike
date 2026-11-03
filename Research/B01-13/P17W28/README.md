# B01-13 | PASS17 / WAVE29 — Existing evidence intake R2 (physical receipts, not new gameplay)

**Owner:** B01-13 HUMAN PLAYTESTING | **HQ:** [#800](https://github.com/C8LyNn/RoboRougelike/issues/800) | **new descendant of own unmerged [W28 #844@4de04e7447c76cebec50f71e64f4e0b52ae928a9](https://github.com/C8LyNn/RoboRougelike/pull/844)**. Reuses W27 [#819](https://github.com/C8LyNn/RoboRougelike/pull/819) real observer output. This is a SOURCE-only functional FIX to the existing W28 intake. No edits to actual Codex R3 game, #828 Saw, #839 EMP, #826 Save, #846 garage, asset producers or protected branches.

## Reproduced W28 real bug and W29 code correction

**Original W28 physical RED:** an arbitrary tiny synthetic ZIP containing only three correctly named fake files `RoboRougelike.exe`, `UnityPlayer.dll`, `RoboRougelike_Data/boot.config` returned `P15_PLAYER_ZIP_BOOT=PASS` with just ONE evidence file. Original W28 correctly labelled actual independent boot `NOT_INDEPENDENTLY_VERIFIED_BY_THIS_TOOL`, but a misleading green operator receipt was still possible.

**W29 R2:** P15 operator PASS now demands **two distinct existing physical files**, a CRC/structure-checked Windows ZIP plus **Player.log** or .txt, each with independently computed SHA256/byte count. A standalone ZIP fails `W29_BOOT_BUNDLE_REQUIRED`. Even with both files, `windowsPlayerBoot=NOT_INDEPENDENTLY_VERIFIED_BY_THIS_TOOL` and product gate HOLD: malicious/synthetic logs cannot be distinguished by this tool. Independent B01-60 must actually run extracted Player, verify its process exit, real gameplay, build SHA and log.

**Softlock #812:** W28 accepted a single log plus self-reported `CONTINUE_VERIFIED` for P12 PASS. W29 requires a 3-file `before`, `after`, `playerLog` evidence bundle with distinct before/after SHA256 and valid relative paths. This is still **operator-self-reported**, not independent proof of a legal continuation. V7 economy, one-shot loaner and deferred rebuild remain unchanged. Absent real Unity evidence and B01-00-approved legal run-end/continuation, **P0 remains unresolved**.

## W29 exact 21-case acceptance matrix

Preserve original 17 case IDs unchanged and add FOUR P0 cases:
- `P18_SAW_DEFAULT_MOUNT_AND_OVERFLOW`: re-test original player and enemy saw mounts, own collision, RPM/heat/energy/damage and overflow diagnostics; #828 currently native Edit 2/4 and Play 19/20, **BLOCKED** until producer repairs
- `P19_EMP_DETACH_DIRECT_GUARD`: accepted exact #839, real energy/cooldown/owner attachment and native Player; source/native targeted #849 passed but independent G0 and integrated game still needed
- `P20_GARAGE_PHYSICAL_ZONE_COMMIT`: five colors including PURPLE/ORANGE/true GREEN, identical real preview/commit, no Scrap/ID loss, grid 0.55m, player performance
- `P21_SAVE_ONE_OWNER_COLD_UNITY`: exact Unity-compatible #826 plus #792 physical world + #780 V7, actual one-owner end-to-end cold save/load, loss ACK, cross-generation integrity

P17 model/turret remains optional P1. All remaining **20 cases mandatory P0**. `BLOCKED` / `NOT_RUN` must not count as PASS. No human trials are invented.

## Input schema R2 and execution

Create source-only empty template:
```bash
python Research/B01-13/P17W28/playtest_intake.py --game-commit <ACTUAL_40_HEX_GAME_COMMIT> --make-template <NEW_ABSOLUTE_OUTPUT_JSON>
```

For ordinary cases, `evidence` is one relative filename. **P15 PASS** `evidence` must be an object:
```json
{"playerZip":"build/RoboRougelike_Windows.zip","playerLog":"logs/Player.log"}
```
**P12 PASS** `evidence` must be:
```json
{"before":"states/before.json","after":"states/after.json","playerLog":"logs/loaner-player.log"}
```
`before` and `after` accept .json/.png/.jpg/.jpeg, require distinct bytes; `playerLog` .log/.txt. P12 FAIL with `softlockDisposition=UNRESOLVED` can still use one physical failure log. W27 observer JSON and 21-case W29 response JSON must have the SAME game commit. The W29 schema is `b01-13-w29-acceptance-r2` (explicit version, no silent W28 interchange). Tool outputs only new `W29_RECEIPT.json` and `W29_RECEIPT.csv`, one CSV row per physical proof (multiple roles retained).

Invoke:
```bash
python Research/B01-13/P17W28/playtest_intake.py --game-commit <EXACT_SHA> --w27-report <ABSOLUTE_OBSERVER_JSON> --w28-responses <ABSOLUTE_R2_21_CASE_JSON> --evidence-dir <ABSOLUTE_EXISTING_EVIDENCE_DIR> --output-dir <ABSOLUTE_EMPTY_SCRATCH_DIR>
```
No network, downloads, Unity launch, game Save writes, user PII or telemetry. Requires safe scratch outside any Git worktree and refuses output overwrite, evidence symlinks/path traversal, missing physical files and wrong case enums. Unique evidence SHA256+bytes are a **receipt**, never game authenticity or verified humans.

## Verified boundaries and receiver

Author's local actual Python 3.13 regression tests **51/51 PASS** in a disposable repo-shaped source checkout plus real five-file SHA/Git-blob manifest verifier **5/5 PASS**. Original old W28 fake ZIP acceptance reproduced separately. Updated own GitHub Windows/Ubuntu Python3.11+3.13 CI must independently run before any remote SOURCE PASS can be claimed. Source-only package; **NATIVE_UNITY_G1=NOT_RUN; LIVE_PLAYER_BUILD=NOT_BUILT_HERE; ACTUAL_HUMAN_SESSIONS=0; G0=PENDING; INTEGRATED=NO; RELEASED=NO; CODEX_ACK=NONE; PRODUCT_GATE=HOLD**.

**Exact current upstream snapshots:** Saw #828 `887fd18bf0a518ee24776c660f5efa6e3a0491d8` source with real native default-hull compatibility/overflow RED per #848. EMP #839 `fea000fe0a706df2b52b6f295bdb3135ba1f9384` real targeted native PASS via #849, not game integrated. Save #826 `99fb08619c78b3834bb4c0450499babf991f218a` source/netstandard2.1/atomic API PASS via #847, not native Unity whole-game G1. Garage #846 gives advisory zones not physical validator. No ability to inspect Codex's local unpushed worktree, installed Windows Unity or user PC through this GitHub-only action.

**Safe rollback:** discard only new W29 unmerged Draft; leave #844, #819, source game, Codex worktrees and user evidence intact. **Next owners:** B01-40 source/G0 exact manifest, B01-60 independent actual Unity/Player and legal recovery session, B01-00 softlock policy, Codex gameplay integrator only after approved intake. DO NOT offer to #79 without G0. 

---

## Original W28 historical documentation (R1, superseded where modified)

# B01-13 / PASS 17 / WAVE 28 — PLAYTEST-EVIDENCE-INTAKE-001/R1

**Owner:** B01-13 HUMAN PLAYTESTING. **HQ:** [#800](https://github.com/C8LyNn/RoboRougelike/issues/800). **Source:** exact inherited W27 [B01-13 #819 @ 65c73fd4a3e6c9fcc61b2e388542b96dbfa1748c](https://github.com/C8LyNn/RoboRougelike/pull/819). New isolated paths `Research/B01-13/P17W28/**` and one own GitHub workflow, no edits to W27 source, Unity, game, Codex, weapons, Save/Load, gameplay, main/develop, any frozen source. The final PR is stacked **only on the immutable original W27 owner branch**; the prior #819 and #786 have not been accepted/merged independently.

## Function physically delivered

An **executable standard-library Python CLI** that imports the real, exported W27 observer JSON and enforces all 8 original records/summary/commit provenance, then gathers a separate 17-case **W28 acceptance witness** matrix. For each claimed operator PASS/FAIL it requires a distinct **existing physical** evidence file (log, picture, video, XML or ZIP), checks path traversal/symlinks/foreign files and measures its real SHA-256 and bytes. It rejects duplicate evidence, a different game commit, unknown keys, false W27 success, malformed W27 rows, fake human certification, and a PASS for softlock without the explicit witnessed **`CONTINUE_VERIFIED` or `RUN_END_VERIFIED`** disposition. For a claimed Player ZIP boot it checks ZIP CRC, presence of `.exe`, UnityPlayer.dll and Unity `_Data/boot.config` or globalgamemanagers, and rejects unsafe ZIP member paths. A syntactically valid ZIP is **NOT** proof that a Windows Player booted. An API-driven two-fight log is **NOT** a manual human playtest.

The CLI writes only two files, `W28_RECEIPT.json` + `.csv`, exclusively into a pre-existing separate empty/outside-Git output directory (`O_EXCL`, no overwrites, no delete), preserving user/game files. Both contain compact per-case statuses, physical evidence file hashes and exact **declared** game SHA; neither contains raw Player logs or screenshots. **No game launch, no Unity compilation, no test-script injection, no network, no player Save access, no screenshot scraping, no first-person user action, no actual human sessions.**

### 17 W28 operator cases

| ID | Witness | Primary owner / purpose |
| --- | --- | --- |
| P01 | Basic / Automatic / Heavy own-part obstruction | Codex projectile exact real Player |
| P02 | Shotgun / Railgun / Rocket blocked + unblocked | Native W27 #824 regression |
| P03 | Beam own part + heat on blocked contact | Continuous weapon, no projectile generalization |
| P04 | Rotary Saw spinning & contacting owned rotating parts | Contact weapon, real Rigidbody |
| P05 | EMP AOE with own geometry & dynamic cover | Area weapon, separate energy/cooldown |
| P06 | Dynamic obstacles, detached moving parts | Frame-by-frame physical recheck |
| P07 | LPM/RPM group, multiple guns | No wasted blocked resources |
| P08 | Five 3D garage zones RED/BLUE/PURPLE/ORANGE/GREEN | Visual/hitbox/attachment distinct |
| P09 | Live 0.55m grid rotation | Placement preview == commit validator |
| P10 | Invalid purchase no Scrap/part mutation | Atomic transaction rollback |
| P11 | Multi-cell wheel/track/turret mechanical envelope | Size != Collider != sweep volume |
| P12 | One-use loaner Win1, no Scrap, Core alive, broken essential Drive | Must prove next legal progression or run end |
| P13 | 65% floor V7 + delayed next-win rebuild | Actual distinct wins, no duplicate |
| P14 | Extracted Windows Player 2 real fights/rewards | Actual Player log, **not API-human claim** |
| P15 | Windows Player ZIP boots | Actual ZIP inspection + operator evidence |
| P16 | Save/load/cold restart | One physical owner, persistent IDs/wallet |
| P17 | Claude Turret v3 dual Muzzle yaw/pitch | P1: retain old turret if invalid |

P01–P16 are P0 observation coverage; P17 P1. All PASS requires **separate actual evidence file** and operator declaration, never host or G1 acceptance. `BLOCKED`/`NOT_RUN` can honestly reflect features missing from tested build. Even full self-reported coverage produces `productGate=HOLD_INDEPENDENT_G0_G1_AND_REAL_HUMAN_ACCEPTANCE`. Software cannot prove that logs are authentic, person performed the steps, Unity tested the target SHA, or a screenshot depicts the same build; **B01-60 independent G1 and human study required**.

## Usage (operator, not automatic execution)

Precondition: download/produce true W27 `B01-13-observation.json` from the game playtest, record the **real game commit SHA** (40 lowercase hex). You may carry the W27 report forward even if its honest status is `INCOMPLETE`; do not replace BLOCKED with synthetic PASS. All paths absolute. Use a private scratch outside repo/game/`persistentDataPath`.

```bash
python Research/B01-13/P17W28/playtest_intake.py \
  --game-commit <40-hex-GAME-COMMIT> --make-template /scratch/W28_RESPONSES.json
```

Edit the generated template's 17 `cases` with **honestly observed** results: `PASS|FAIL|BLOCKED|NOT_RUN`, pre-defined `reason` and a unique relative `evidence` filename for each PASS/FAIL; files must physically exist in a separate `/scratch/evidence` directory. For P12 you must also set `softlockDisposition=CONTINUE_VERIFIED|RUN_END_VERIFIED` only after actual proof, or `UNRESOLVED`. For cases without execution keep `NOT_RUN` + `reason=NONE`, `evidence=null`.

```bash
python Research/B01-13/P17W28/playtest_intake.py \
  --game-commit <40-hex-GAME-COMMIT> \
  --w27-report /scratch/B01-13-observation.json \
  --w28-responses /scratch/W28_RESPONSES.json \
  --evidence-dir /scratch/evidence \
  --output-dir /scratch/receipt
```

**Never treat `--game-commit` as authenticated by the CLI**: it is operator-declared. A `.log` file or syntactically valid ZIP is **physical bytes, not a signed Unity execution result**. The inspected W27 original schema is `b01-13-observer-r1`, original W27 source and its 48/48 Node / real Chromium 5/5 CI were evidenced separately at exact SHA; W28 does not rerun those tests or claim human sessions.

## Test and acceptance

```bash
python Research/B01-13/P17W28/verify_package.py
python -m unittest -v Research/B01-13/P17W28/test_playtest_intake.py
```

Dedicated Actions matrix: **Windows 2025 / Ubuntu 24.04 × Python 3.11 / Python 3.13**, no Unity install. Genuine test code asserts deliberately fabricated fixture bytes as **SYNTHETIC TEST DATA ONLY**, including a fake Unity-structured ZIP; it explicitly verifies that such an archive does NOT get counted as boot verification. Physical output and anti-overwrite tested in disposable temporary directories. Tests never produce actual real player evidence.

**AC:** input must reference exact current game SHA and real W27 JSON, no phantom human counts; missing/invalid evidence FAIL_CLOSED, softlock PASS without continuation or run-end proof FAIL_CLOSED, malformed/traversal ZIP refused; outputs only in owned scratch. Expected producer result if tests pass: `IMPLEMENTED=YES; TESTED=SOURCE_ONLY; QA_ACCEPTED=NO; G0=PENDING; NATIVE_G1=NOT_RUN; GAME_ACCEPTED=NO; RELEASED=NO; REAL_HUMAN=0; CODEX_ACK=NONE; G2=HOLD`. Source manifest pins 4 code/docs files + one workflow physical SHA1 Git blob and UTF-8 bytes (5 nonself paths), no overwrites to W27.

**Recipients:** B01-40 exact source-only manifest G0, B01-60 independent Unity/Windows Player/log/manual QA, B01-00 game gates and P0 softlock owner decision, B01-22/20 authoritative loaner/ECON owner, Codex remains sole integrator after accepted G0. Do not auto-post to #79 before G0, do not merge or release. Safe rollback: close isolated Draft; no writes to protected branches or live game saves, do not delete human observation exports.


---

## W31 R3: archive limit fix and coherent Windows Player evidence

**Same original implementation:** changes only to inherited B01-13 playtest_intake.py, tests, README, manifest and workflow. No new game, validator, branch-family Save writer, garage logic or Codex worktree changes.

**ACTUAL ORIGINAL RED:** Original W29 R2 refused a real physical 66 MiB ZIP with FILE_SIZE because all submitted evidence had a 64 MiB cap. New implementation raises the cap only for ZIP evidence to 2 GiB input bytes and streams checks. Other proof remains capped at 64 MiB. ZIP contents are never extracted; existing declared uncompressed size guard (<3 GiB) persists.

**New P15 PASS requirement:** Three separate existing physical evidence files with evidence roles playerZip, playerLog, sessionReceipt. sessionReceipt must be the source-compatible output of the existing consent-gated W30 B01-13 operator tool #886. This validator binds the W30 declared gameCommit to W27/W29, checks process exit=0, nonempty log status, non-verified-G1 provenance and UTC order, hashes physical Player.log and binary EXE *inside the ZIP*, checks both against the reported W30 original output hash/bytes, and requires its UnityPlayer.dll and corresponding EXE_Data directory on the same ZIP level. Player ZIP can be larger than 64 MiB. Any missing/stale/replaced artifact -> FAIL_CLOSED. Same 21 W29 case IDs, same 8 W27 human-observation tasks and P12 softlock-before/after gate.

**Critical limit:** matching physical bytes and operator-asserted JSON do not authenticate a Windows process, Unity, an embedded game commit, a human participant, player gameplay, or a successful game release. A malicious user could fabricate all three input files. Independent B01-60 execution/review is necessary. Source tests on dummy EXE/log archives never invoke Unity, Windows Player or any human. Always productGate HOLD_INDEPENDENT_G0_G1_AND_REAL_HUMAN_ACCEPTANCE and nativeUnityG1 NOT_VERIFIED.

**R3 regression suite:** 51 inherited + 12 targeted = 63 Python source checks on each requested runner (Windows 2025, Ubuntu 24.04; Python 3.11/3.13). Cases include 66 MiB real ZIP, switched EXE/log after receipt, mismatched commit, invalid session schema/time, fake Unity success claim, wrong sibling DLL and missing receipt. These counts describe source test cases only; final CI result must be verified independently. Source G0 receiver B01-40, native G1/Player B01-60, release owner B01-00. No protected merge or release.


## W32 R4 — unambiguous Windows Player ZIP evidence

**Parent:** B01-13 W31 PR #905, exact immutable commit 7309b8b080041f984cae3adcdd8b2d08bb2f3ea0. Same W27/W28/W29 intake and existing W30 process receipt; not a new game or a second authority.

**Reproduced physical W31 RED:** A structurally valid ZIP contains two RoboRougelike.exe entries with identical filenames but different binary bytes. W31 can hash the first, match W30 session receipt and report evidence consistency, yet standard filename-based extraction selects the second. An operator case P15 PASS on this archive is therefore unsafe/ambiguous. The exact original W31 tool accepted the archive and mismatched extracted identity.

**R4 fix:** Original existing playtest_intake.py now validates every ZIP member's canonical Windows extraction name: exact and case-insensitive duplicates, file-vs-directory collisions, Unix symlink members, Windows unsafe characters, NTFS alternate streams, trailing dot/space and reserved DOS device names are fail-closed. The check applies both during archive inspection and direct session-receipt verification. Original 2 GiB ZIP limit, CRC test, traversal checks, physical EXE SHA256, Player.log SHA256 and W30 receipt remain intact. Legitimate UnityCrashHandler64.exe and an archive rooted in a nested directory still pass structure-only screening.

**Tests:** 9 added source-only tests at Research/B01-13/P17W32/test_zip_canonical.py, seven new security/canonicality tests were RED with original W31, two positive cases PASS; all 9 PASS after source fix. Original 63 W31 tests unchanged, combined 72/72 locally PASS. Remote GitHub Actions is a separate source check. No tests ran the real Unity Editor or Player. Fake ZIP bytes do not certify genuine Unity gameplay, a human participant or Game G1. Independent B01-40/60 native QA remains mandatory.

**Other W32 blockers:** current selected #877 is only SOURCE plus fail-closed native gate, #896 is one-writer source update not game-integrated, #897 reports 65% floor/paid-zero R3 rebuild pricing RED, #834 owner softlock decision pending. This PR neither changes gameplay nor gives false acceptance.
