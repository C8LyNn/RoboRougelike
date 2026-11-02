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
