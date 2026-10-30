# B01-13 — P17/W25 — APPROVED-GARAGE-HUMAN-STUDY-001/R2

**Owner:** B01-13 Human Playtesting. **HQ:** #731 owner policy (2026-10-30). A practical, independently runnable, single-file Polish offline browser study, not a clone of the W20–W23 speculative economy/path solvers. No app connector, game state, network, participant identifiers, Unity install or Codex integration.

## Actual implemented end-user feature

The original executable `study.mjs` generates a **real HTML browser application**. The person must voluntarily check TWO separate consent/privacy statements before starting. Eight cards show carefully differentiated approved 0HP garage rules (Light can deploy with optional broken components, Heavy requires functional drive, Core dead, next challenge missing, loaner already consumed, new same-win damage, duplicate win ID, authentic one-shot weak-loaner win). Each card has five compulsory multiple-choice questions: normal deploy, one-time weaker loaner eligibility, quote at exactly 65% of new-purchase price, whether an older pending rebuild completes on the described event, and whether a fresh free entitlement appears. The participant may skip a case, erase the study without saving, or download anonymized locally generated JSON and CSV after completing the full eight-card study. The app reports exact answered/skipped/correct maximum counters and per-question accuracy in JSON. No free-text, login, account, email, phone, IP, persistent browser storage or automatic network calls.

**Run** Node20+ `node Research/B01-13/P17V/study.mjs --output /absolute/operator-owned-safe/study.html` (output must be outside ANY git checkout, file must not already exist). Alternatively download the verified CI artifact containing the already-generated self-contained `study.html` and open in a browser. No Node needed for participants. It has one static inlined original controller with exact CSP `sha256`, no CSS/font/JS assets loaded online. Generated HTML uses private 0600 filesystem permission where supported and exclusive create+fsync, refuses overwrites and Git worktree output.

## Policy handoff — supersedes W20–W23 hypothetical study assumptions

Owner approval stated in the TOP of #731 is now authoritative, no longer a provisional policy. Testable rules:

* one owner-global free rebuild reservation for a **genuine distinct verified** victory, no stacking/replay/loss/duplicate receipts; newly destroyed components cannot complete on the same victory but wait for a later authentic victory;
* a temporary WEAKER loaner is APPROVED once per run only when own vehicle is truly non-functional, Core alive, and next authentic challenge exists; no second loaner after loss or consumed attempt, no inventory transfer or duplicate reward;
* rebuilt destroyed 0HP component costs 65% of equivalent NEW part purchase. The eight cards deliberately use new-item prices **100 or 200 Scrap**, for exact unambiguous 65/130 sample quotes. Positive integer prices like 1, 5, 15 and 30 need a deterministic, owner-approved rounding/clamping contract from **B01-22**, NOT secretly chosen by the study module;
* normal challenge blocked only for truly dysfunctional robot (trusted Core/required drive/gun/structure), never because any optional 0HP module is damaged; actual server/gameplay must recheck under its single lock;
* Save must be honest-crash/corruption resistant; sophisticated malicious on-disk anti-tamper is explicitly NOT a release requirement, but normal API idempotence, owner `.owner` lock, actual full-world Save and loss/ACK regression remain P0.

Source state indicators are fictional test-case facts, not a live `CanDeploy` or ECON authority. The browser's answers DO NOT call the real game, mint genuine events, grant Scrap, save Unity objects, take ownership of the selected #749 bridge, or stop Save rollback. A `UNIQUE_WIN` card describes a synthetic **authentic outcome** hypothetical; it does not create real combat authority.

## Honest evidence and privacy

**Actual human participants who played in this ChatGPT/producer turn = 0.** Automated Chromium interactions and exported trial JSON/CSV are **test fixtures, not human data**. No consent is obtained from an actual user in producer CI. Any future real human test must be conducted by an authorized human study operator who explains purpose, gets freely given informed consent, allows stopping, never coerces contact details, and stores any downloaded results securely under approved rules. The software doesn't verify the legal identity or actual humanity of the operator; the JSON clearly labels local client responses as unverified. No identifiers or external telemetry. Browser refresh or erase loses everything not voluntarily downloaded; no auto-save in any game/production disk.

## Reproducible genuine test gates

`node --test Research/B01-13/P17V/test_study.mjs` executes 44 actual Node source test cases: 8 hand-reviewed example oracles, next-distinct win vs current new damage/loss/replay, Core/no challenge/loaner one-shot, 65% exact prices, scoring, malformed records, immutable/privacy declarations, exact CSP, physical CLI/exclusive create/refusal. `.github/workflows/b01-13-w24-approved-garage-study.yml` runs this on both Ubuntu24.04/Windows2025 and Node20/22, checks exact Git blob SHA1 and original file byte sizes for all nonself package files, prints SHA256 and generates a real browser artifact.

An independent `browser_test.py` runs **actual Chromium via Python Playwright on Ubuntu**, with 8 real selectable tasks and 7 submitted answer sets plus one skipped case, consent gating, incomplete-form rejection, real CSV and JSON downloads, score values and wipe/reset, desktop/mobile layout and zero browser JS errors. GitHub CI must show original actual browser success separately. The browser test uses an isolated synthetic series, NEVER participant outcomes. Native Unity 6000.3.14f1 EditMode/PlayMode/PhysX and actual gameplay integration remain **G1_NOT_RUN** until owner-operated licensed Unity evidence exists.

## Git security, dependencies, receiver and rollback

New original **standalone additive** package on unmodified main, NO dependency on speculative old #722/#738 implementation branches. Earlier #722 and #738 are historical frozen source evidence with older *provisional* policy wording; do not install them as owner-approved future gameplay. The W24 package changes only `Research/B01-13/P17V/**` and one own GitHub workflow. No frozen gameplay #510, Save #556, R5 #690, selected V3 #729, bridge #749, API/ECON source or alternative #753/#741 modifications. No ALT_HOLD combination, protected branch push, merge or direct user PC access.

**Intake** B01-40 independent original SHA+6 own paths and manifest physical source G0. **Gameplay/UX** B01-10/B01-20/B01-22 review scenario decisions and the still-open rounding rule. **Native** B01-60 real owner-operated Unity G1 plus any consented in-game player study. **Coordination** B01-00 sees final receipt. **Rollback** discard only this unmerged Draft and its operator-owned disposable HTML, never source, other Git worktrees or real participant data. `SOURCE_G0=PENDING`, `NATIVE_G1=NOT_RUN`, `INTEGRATED=NO`, `RELEASED=NO`, `CODEX_ACK=NONE`.


## W25 R2 — confirmed keyboard-focus accessibility bug, exact source RED→GREEN

**Versioned own-source descendant** from immutable W24 [#769@445d84c7f5a5209cbe92219191b6723bc64d6421](https://github.com/C8LyNn/RoboRougelike/pull/769). Reuses the SAME original interactive browser study: consent, approved 8 cases, 40 questions, optional skip, objective scoring, physical private CSV/JSON exports and erase. No another economy prototype or actual Unity gameplay changes; only already-own P17V files changed. Main remains untouched; new Draft targets the exact W24 OWN branch and cannot be source-installed without its base being accepted separately.

**Real Chromium defect:** after Start, keyboard focus was left on the now-hidden Start button; after Skip/Answer the next card heading wasn't focused; after Finish, focus stayed on now-hidden form control; after Erase/Stop focus stayed in now-hidden result panel. Actual baseline W24 Chromium prints `START_FOCUS_NOT_ON_TASK_HEADING` and fails when tested against NEW keyboard assertions. Old R1 snapshot remains unchanged at exact commit 445d84c7f5a5209cbe92219191b6723bc64d6421.

**Real R2 repair:** three document headings with `tabindex=-1` and the existing controller now explicitly sets focus on the NEW visible task heading at every card transition, visible summary heading on finish and consent heading on erase/stop. `tabindex=-1` ensures no extra keyboard-tab stops. Tests: original 44 Node cases plus FOUR new focus-specific source assertions = 48, actual independent Playwright Chromium through all 8 cards, skip/answer, download JSON+CSV, mobile and erase, with focus checked on all transitions. Expected-RED old W24 and GREEN new W25 are browser results, **not real Unity G1 or observed participants**.

**Owner-approved economy unchanged.** B01-10 #686 favors floor(65% of new) with 1→0 strictly cheaper; B01-22 #764 used nearest half-up with 1→1 (NOT strictly cheaper). Exact price=1 rounding clash must be resolved by actual economy owner B01-10/B01-22, not by a UX tool. This study intentionally uses only new prices 100/200 giving unambiguous 65/130; no altered fiscal/game authoritative contract. One-free-per-authenticated-win and one-use weaker loaner remain approved, but executable Unity authority remains unverified.

**Source G0** separate B01-40 exact R2 manifest, **Native Unity G1 NOT_RUN**, **Human participants 0**, **CODEX_ACK NONE**, **G2 HOLD**. Receivers B01-11/12 UX visibility, B01-40 scope/manifest, B01-60 independent accessibility/native/human QA, B01-10/B01-22 price-policy wording, B01-00 coordinated intake. Safe rollback abandons unmerged stacked Draft, not old W24 or user exports.
