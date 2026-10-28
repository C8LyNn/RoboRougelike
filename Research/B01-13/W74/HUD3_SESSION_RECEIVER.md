# B01-13 — PASS15 continuation: HUD3 A/B synthetic participant-session receiver

**Research-only, one executable fixture.** This is a B01-13 independent receiver, NOT a source edit to B01-12, not a new Unity package, and not any real-human result. Checks the **unaltered actual B01-12 HUD3 HTML** in Chromium, and an operator-side volatile synthetic withdrawal flow. A check marked `FAIL` is a **real browser finding on the original B01-12 HTML**, not an operator-tool passing assertion.

## Inputs / exact source identity

- **Owner:** B01-12; original HTML: `Research/B01-12/W70/HUD3-overflow.html`, research branch `research/b01-12-w70-hud3-overflow`, Git blob **`1caee60184a918d1690a67e363acfc8b213bfe9b`** at observed commit `862bff8c876f977859dc0a1ebd6e8a507046310f` (source status checked before run). The branch `research/b01-12-w74-garage-readiness` also contains the **same** original HUD3 blob as of this test. This is not a revised B01-12 source; source owner must publish the fix first.
- **Codex game #310:** `231005c6d0351945a8ee6684f3856af118f0e94f`. Its Unity HUD and physics are **not** exercised by this test.
- **Existing independent B01-11 findings already known:** treatment label A/B visible to participants and incorrect “rozwinąć” hint on A F0 (issue #296 comment `6464206638`); report but do not relabel them as new bugs.
- **NEW B01-13 P15 observation:** after moderator `#resolveA` changes fixture F1 and closes P01 session, entering synthetic P02 session without a fixture change **retains the prior mutated weapon state** despite the selected F1 scenario being the same. This may bias A/B comparability. Owner correction suggestion: on **each participant-mode entry**, restore a copy of the selected immutable fixture and collapse expansion, or require an explicit protected operator reset before opening a new session. Do not decide owner design unilaterally.

## Reproduce in a real Chromium

Requires Python 3.11+, `pip install playwright`, Chromium at `/usr/bin/chromium` (Linux test host). Work from a checkout that contains **B01-12's original HTML** and **B01-13's** new file. No external HTTP is requested by the HTML or runner.

```bash
python3 Research/B01-13/W74/hud3_participant_flow.py \
  --html Research/B01-12/W70/HUD3-overflow.html \
  --expected-git-blob 1caee60184a918d1690a67e363acfc8b213bfe9b \
  --out /tmp/b01-13-hud3
# Expected exit: 3 = original SOURCE_RED, not an approved participant stimulus.
```

Use `--accept-known-red` only to execute diagnostics with exit 0 while keeping `research_ui_contract_pass=false` and 12 producer `FAIL` rows in the JSON. This switch does **NOT** accept the research UI or authorize participants. Default is fail-closed.

**Tests actually executed on source blob above:** 61 browser+volatile-session assertions across CSS 320/720/1280: **49 PASS / 12 FAIL**. Nine failures are B01-11 previously established findings replicated on 3 widths (2 treatment labels + A F0 misleading expand hint). The **new** distinct issue is stale F1 state across P01→P02 sessions (3 widths). Passing checks include identical A/B group totals when switching mode, hiding actual moderator controls, B disclosure keyboard+Escape cleanup, no horizontal scroll, fresh browser context, no network, and synthetic P01 withdrawal followed by P02 and rejection of P01 re-entry. Withdrawals are **operator-side in-memory synthetic-only**; the independently delivered [B01-13 PASS15 intake tool](https://github.com/C8LyNn/RoboRougelike/blob/d6283a5ac4099424aa416b9e11e511f5473a2bd9/Research/B01-13/W74/hpt13_intake.py) handles persistent offline CSV revocation for actual consented studies after HQ authorization. This new runner does not replace it.

**Harness positive control:** I separately made a **local, uncommitted, deliberately modified sample** fixing treatment label visibility, truthful F0 instruction, and session reset; the *same unmodified receiver* accepted it with **61/61 PASS**, Git blob `60cbf332ff894295f8e9d93242ead58f6496a9dd`. **That local mutated HTML is NOT B01-12-approved, not an original GitHub source, not game UI, and not a shipped fix.** It was not uploaded to any production branch and may not serve as B01-12's source SHA.

## Output / review protocol

`--out` directory receives `HUD3_B01-13_RECEIVER_SYNTHETIC.json` with every named positive/failure and 720px original browser screenshots `HUD3_A_F1_720_SYNTHETIC.png` / `HUD3_B_F1_720_SYNTHETIC.png`. Never substitute screenshots for player experience or claim source PASS when JSON has failures.

Once **B01-12 publishes an actual revised HTML blob**, pass **exact new Git blob SHA** using `--expected-git-blob`, repeat the *same tests* and let B01-11 independently recheck its separate treatment-label A/F0 failures. No source-writing to B01-12 or Codex. No G0/G1/G2 approval. No real participants (0).

## Exit semantics and scope

- `0`: **UI receiver passed**, or diagnostic override via `--accept-known-red` with detailed explicit FAIL (read JSON!); never study authorization.
- `3`: actual browser fixture ran but one or more B01-12 producer conditions failed — **the current original default**.
- `2`: source byte/expected Git blob mismatch or runtime error, **no valid assessment**.

No real recruitment/participant sessions without explicit HQ GATE D consent, private storage and authorized Windows Player operator. HUD3 itself is a browser *research* stimulus, not integrated into RoboRougelike.
