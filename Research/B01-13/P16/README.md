# B01-13 — HPT16-GBC01-R1 / PASS 16 / Garaż → Budowanie → Walka

**STATUS: working research-only browser tool, NOT game-integrated, no real human research, no Unity native run.** This is not a game code package competing with Codex, CHL-003 or PARTSIZE. It adds a distinct *chained* three-task moderated workflow to older independent HPT-T01…T05. The older HPT13 v1 scoring and 5-task CSV intake are **not compatible with the new 3-task JSON**: do not combine files.

## Produced original files

- `HPT16-GBC01.html`: standalone, functional, self-contained study operator/player prompt UI; no network, no localStorage or cookie sessions. Real timer with `performance.now()`; explicitly operator-labelled outcomes, errors and hints; neutral participant-only view; Escape to moderator; export JSON only after all three timed observations; withdrawal purges all current-session memory and bars exporting. Previously exported copies need the separate offline intake delete flow.
- `GBC01-protocol.json`: exact source-anchored, machine-readable *three* neutral participant tasks and separate facilitator-only setup, expected evidence and pass/fail criteria.
- `hpt16_intake.py`: local-only JSON gate for 3-task exports (different from `W74/hpt13_intake.py`), blocks wrong IDs, source/build mismatch, outcomes, invalid timing, mixed SYNTHETIC/HUMAN and builds. `revoke` writes durable PID tombstone **first**, then deletes only matching local exported files; old copies elsewhere are not erased. It cannot authenticate consent.
- `test_gbc01_browser.py`: actual Chromium/Playwright executes **original HTML JS** with synthetic P01 and deliberate outcome events, captures actual Download/JSON, compares full protocol, tests prompts, timing, UX switch while timer runs, keyboard, invalid approval, withdrawn/replay and 320/720/1080 responsive. The test environment blocks Chromium `file://`, so it executes exact original HTML bytes via `page.set_content` (NOT a JS translation). Uses `/usr/bin/chromium` in current Linux test host; on Windows change executable argument to Chrome/Edge actual installation.
- `test_gbc01_intake.py`: exact original JSON exported by real Chromium is fed to actual original Python intake. Positive and negative (malformed, replayed, consent metadata, mixed builds/modes, withdrawal) local-file tests.
- `QA_BROWSER_PASS16.json`, `QA_SYNTHETIC_GBC01.json`, two explicitly **SYNTHETIC** PNG screenshots, `QA_RUN_LOG.txt`: **synthetic tool test evidence**, not real human or gameplay observations. Public repository stores only source and synthetic evidence.

## Game anchors — exact Codex published source, NOT simulated gameplay

Input Codex [PR #310](https://github.com/C8LyNn/RoboRougelike/pull/310) at **`589b60a912096d1e9917cb0bbac73a53e7543b4f`** (published head inspected 2026-10-28). Explicit C# blob anchors live inside `GBC01-protocol.json`:

- Menu and garage/UI: `R1RunHUD.cs` blob `1c76e1cc6b393b14450b30dbce75c731c5d95dbe` — **Garaż i poligon testowy**, `Złom`, `Zamień A / B`, movement; test range LPM/PPM controls.
- Physical garage and rules: `R3VehicleModel.cs` blob `8cc68faf89793c3a803e5807594c9faf0df10091`, `R3GarageView.cs` blob `fb6f4f21c2f3953e43e1a7b223ef5110dbcc26c1` — buy/mount/remove, two distinct weapon families, contact rules, `CanDeploy`.
- Isolated sandbox: `R6RangeController.cs` blob `cc7082e54306fdd0a5506f07e5d419cadd24c292` — `OpenSandboxGarage()` and `EnterTestRange()`; test world uses a model copy, no persistent garage loss by design.
- Range HUD: `R6RangeHUD.cs` blob `adfd3c1a69fc4355e055581a736ce10cccc7ba91` — group A/LPM, B/PPM and pause-return guidance.

**Important:** This tool is not connected to the game process or to input telemetry. An operator observes actual player behavior and records it; never assume that a clicked research button drove a car or fired a shot. The participant prompt does not reveal button names or answer keys.

## Participant setup (actual human research CLOSED until explicit HQ GATE D)

A real pilot needs **independent** written `HUMAN_PLAYTEST: APPROVE`, adult volunteer participant(s), private actual consent, assigned facilitator/operator, approved data destination and retention/deletion policy, and an authorized isolated Windows Player of the exact confirmed build hash. START AUTHORIZED for the swarm does not authorize recruiting humans or access to a licensed environment. `HUMAN_APPROVED` form is a structural gate only; reference fields cannot prove approval happened.

Operator must independently verify: a fresh instance of Light in its newly opened sandbox garage; no prior save/real study residuals; money and two mounting kinds/slots available; `EnterTestRange` physical outcome; return from paused test range without loss; actual group A/B. If the product truly cannot configure two distinct family guns, record `BLOCKED_SETUP` and STOP; do not secretly buy parts for the participant or repurpose a different runtime. Keep recordings OFF unless separately consented.

### GBC01 three sequential participant prompts

| Stage | Neutral participant goal | Facilitator observation and *not simulated* pass condition |
|---|---|---|
| G01 — Garage, 3 min | Find where the vehicle can be viewed and modified | Actual garage opened, UI and physical vehicle visible |
| B02 — Build, 6 min | Equip **two different types of weapons** and keep vehicle driveable | Actual source-backed two distinct R4WeaponKind mounted, CanDeploy still true, correct A/B status and no hidden assistance |
| C03 — Combat range, 6 min | Run training, drive, use both weapon groups, return to workshop | Real independent A/B fire and vehicle physics evidence, return to original garage with preserved parts; actual combat hit must be *observed*, not inferred |

Per step, record actual elapsed time, observer outcome (unaided, assisted, fail, product blocked, setup blocked), error count, help count and optional anonymous technical note. For real users, consent can be withdrawn at any point; local JS is immediately purged. False positives are possible if a moderator lies: the tool does not claim to verify product behavior or consent authenticity. 

## How to run offline / local data folder

From the research branch checkout, open the standalone `Research/B01-13/P16/HPT16-GBC01.html` in a regular browser, **on a separate operator display**; share only participant mode with participant. Before session, enter *full* real source/build SHA40 and actual Player ZIP SHA256 (not guessed). For synthetic setup you may use dummy `a*40` and `b*64`, but **never publish such data as human observations**. Export goes to browser Downloads as `GBC01_SYNTHETIC_P01_S01.json` or the approved human mode naming; move only approved exports into an isolated **private** operator folder. Do **not** place human session data in repo/Issues.

```bash
# Real browser test (synthetic, no humans):
python3 Research/B01-13/P16/test_gbc01_browser.py
# Test original exported JSON and local revocation:
python3 Research/B01-13/P16/test_gbc01_intake.py

# Operator data (private, with legitimate study permission):
python3 Research/B01-13/P16/hpt16_intake.py verify \
  --root /private/GBC01-study --protocol Research/B01-13/P16/GBC01-protocol.json

# Immediate participant withdrawal: durable local tombstone followed by deletion:
python3 Research/B01-13/P16/hpt16_intake.py revoke \
  --root /private/GBC01-study --pid P01 --confirm-local-delete
```

Exit `0` from `verify` = **STRUCTURAL_PASS_NOT_HUMAN_OR_UNITY_PROOF**. Fail closed `2` = missing/invalid receipt or withdraw replay, not a gameplay fail. Deleting a local JSON is **not secure erasure of backups, exported prior copies, browser caches, cloud/email or external recipients**; follow HQ privacy policy and remove each external copy. A withdrawal tombstone must be preserved; if a process crashes mid-purge, verify fails closed on remaining withdrawn records.

## Actual executed tests / exclusions

- Original Chromium browser HTML **RED 1 actual source defect**: active timer disallowed switching into participant-only screen; corrected exact own HTML, then full **42/42 real browser assertions PASS**; synthetic P01 test JSON downloaded via Chromium, not synthesized solely by Python.
- Original Python intake on that physically downloaded original JSON: **28/28 actual process/file scenario checks PASS**, including fail-closed negatives and withdrawal tombstone/replay.
- Visual: screenshots inspected at 1280px participant mode and 720px operator mode. Horizontal-scroll checks actual Chromium at 320,720,1080 PASS.
- `Assets/**` **unmodified**, no imported Unity package, no actual vehicle controlled, no real human sessions or real consent, no independent G1/G2, no new Codex ACK; game still requires real operator/QA.

## Codex / HQ handoff (do not duplicate CV-03 or CV-07)

**Existing Codex #79** already has CV-03 garage and CV-07 GUI/smoke. New human-specific downstream acceptance: only if authorized Player and HQ consent exist, run G01→B02→C03 on a fresh real Light, record independent actual state screens / result timestamps and source/build hashes. If no approved people, Codex may optionally visually preflight **only** fixture setup, without labeling it a human session; no new CV task until a genuinely uncovered native blocker is discovered. Once Codex publishes an updated real build SHA and binary, **B01-13** repins protocol to that exact source in its own research version; **B01-60** reviews the native evidence separately; **B01-00 HQ** controls human recruitment.
