# B01-10 / ECONBAL-001 — measurable balance + player-study acceptance (W20)

## The exact unresolved question

**C1 — Free reservation cadence:** selected owner hypothesis is *one free reservation globally per authenticated WIN epoch* versus *one free per run*, not one free per module or service instance. Only the authoritative, idempotent owner event journal may decide `win`; the offline model accepts its synthetic boolean as input and cannot prove that a real event was authentic. Consecutive losses do NOT regenerate a coupon or complete queued rebuilds.

**C2 — Epoch-zero bootstrap:** original frozen #252 starts `FreeSlotUsedAtWin=-1` with `wins=0`, permitting one free reservation before first victory. Compare this with `bootstrap=false` (nothing free until real first win). Do **not** remove this without conscious owner decision and early-run softlock human checks.

**C3 — Paid quote:** controlled test grid 65%, 70%, 75% of a **source-given purchase price**; discounted quote strictly positive and below purchase price. Test quote pinned at reservation time; no retroactive repricing. No recommendation to hardcode 70%; actual game wallet inflow, shop stock and per-part upgrade cost require native telemetry.

**C4 — Critical mobility:** if funds are insufficient, a limited free coupon should be evaluated for a destroyed mobility module before a cosmetic. A reserved 0HP part stays unavailable for deploy; this policy alone **cannot** heal it without the NEXT genuine victory. Require a separate approved loaner/bypass or safe escape route under #154; no invented instant repair. The model's `attempts_with_mobility_unavailable` measures an *indicator*, not proof of softlock.

## Proposed honest real-human playtest — all NOT_RUN

**Recruitment / instrumentation:** compare 2×2 (per-win vs per-run) × (bootstrap vs none) under matched deterministic actual-game seeded encounters, same starting wallet and parts; require real approved owners/players, consent, actual native G1 and live R3 port. Log participant ID pseudonym, run, encounter ID, actual unique victory ID, starting Scrap, paid quote and snapshot, free-token use, destroyed roles, queued/resolved parts, regain-mobility escape path, win/loss and retry; collect no personal identifiers. Freeze build/physics/seed mapping and annotate any fallback bypass.

**Primary outcomes (per condition):** fraction of players with deployable mobility after each victory/loss; number of runs stalled for lack of mobility before next winnable challenge; Scrap end balance and 0-Scrap time; proportion of queued parts left unrecoverable at run end; rebuilds per win and per run; first-win frustration and completion. Report medians and dispersion by small+heavy rigs, no fake significance from 0 participants.

**Risk thresholds to propose for owner validation, NOT approvals:** zero hard softlocks (critical), zero free-token duplication across two owner service objects, zero stale victory replay after save rollback, zero resurrection without a genuine win, zero price over purchase, visible price and free-coupon promise. Choose quote/cadence **after** this observed evidence and a formal anti-softlock escape review, not from the toy model's arithmetic.

## Test boundary

The 45 Python tests and seeded 200×12 traces intentionally do NOT exercise original Unity `RebuildQueueService`, B01-22 V2 CAS engine, FileCheckpointStore, mono lifecycle, authentic post-win state, actual available socket collision or gameplay HP. The owner's #673/ #620 provide distinct real-source C# execution/negative evidence. Unity 6000.3.14f1 activated Editor.log/PlayMode XML, current game scene and real players remain **NOT_RUN**.

**Independent acceptance owners:** B01-22 economics implementation, B01-13 UX/readability, B01-40 physical intake, B01-60 actual native QA, B01-00 game-design authority. This PR is designed as an offline reproducible research input to that process, not an alternate gameplay runtime candidate.

## W21 specific hard-failure risk addition — eligibility BEFORE next challenge

R1 design-model ordering incorrectly allowed damage created at the end of WIN X to be completed by WIN X itself. R2 resolves only reservations from before WIN X, queues end-of-WIN X damage for WIN X+1. **Acceptance implication:** actual repair screen must display that this part remains unusable until the NEXT eligible genuine victory; never display it as instantly restored after the same win. Verify the original code on a native replay where a wheel/critical gun is destroyed during a victory; confirm its physical 0HP and reservation persist into garage, through defeat, and no restore before the next distinct win.

**Counterfactual fail-closed gate:** Set a distinct `blocks_deploy=true` explicitly only from authentic `CanDeploy` evidence; do not infer deploy failure from any single missing wheel. With critical 0HP and an inaccessible challenge, optimistic model-scripted victory is invalid even if synthetic test input says `won:true`. The model's `blocked_deploy_attempts` and `contradicted_scripted_wins` quantify the inconsistency, NOT the real game's softlock rate. The borrower option `loaner_challenges=1` is a **hypothetical scenario of unapproved policy #154 C**; loaner losing the challenge must leave real owned parts 0HP, issue no free refill, and cannot be freely rearmed/duplicated by retry/save.

**Real game acceptance — still NOT_RUN:** Under actual Unity/G1, exercise Win→new critical 0HP→Garage blocked→genuine next-eligible challenge or approved loaner→Win→reserved parts rebuild exactly once, under both Light/Heavy vehicle types. Test defeat/no victory, double service, stale checkpoint restore, duplicate winning event, wallet conservation and cancellation/rollback in B01-22's source scope. No 2×2 cadence choice or 65/70/75 quote shipping approval until actual non-softlocking player observations.

## W22 Core + next challenge preconditions — additional owner review required

1. An essential 0HP part is **not itself** sufficient evidence of a softlock. Verify real authoritative `CanDeploy` using component physical identity and actual remaining assembly. A source demo saying `blocks_deploy:true` is an input assumption, not game telemetry.
2. Candidate #154 loaner **must require both** authenticated Core alive and a real next challenge currently eligible. If Core is terminally lost or there is no next challenge, no loaner, victory, free quota, Scrap reward, or direct HP repair is allowed. Ensure UI distinguishes *run ended* vs *no next challenge offered* vs *essential part blocks deploy*.
3. If a loaner trial is approved later, issue a single unique nonreplayable lease scoped to the owner+run+eligible encounter and check its remaining quota from an authoritative durable host—not client-side UI or model. Loaner defeat leaves owned 0HP and reservations exactly as-is. Loaner genuine trusted win may restore **preexisting** reserved parts, never those destroyed in that very victory.
4. Human test both Light/Heavy cases including dead Core, no eligible next challenge, damaged but deployable nonessential wheel, destroyed mandatory drive and weapon, no wallet, and 2 services sharing one host. Actual checkpoint rollback and two-process source security belong to B01-22/V3 and B01-62 trusted ledger; this Python model cannot prove it.
5. Measure **real** runs stranded without a next eligible battle, not only wallet or queued count. Do not approve one-free-per-win, 70%-quote, or the one-loaner policy on zero participants.

**R3 evidence scope:** 95 original Python source tests, 10 hypothetical scenarios, 200 fixed-seed invented runs, separate exact-SHA real Windows+Ubuntu workflow. These are testable design invariants, not a live game G0/Unity G1/received Codex ACK. Next owner: B01-00 + B01-22/13 for policy, B01-40 source G0, B01-60 licensed native G1.
