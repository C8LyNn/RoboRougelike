# B01-13 — P17/W24 — APPROVED-GARAGE-HUMAN-STUDY-001/R1

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
