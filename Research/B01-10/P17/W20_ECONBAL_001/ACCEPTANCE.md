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

## W23 — payable service timing & terminal deferred repair consumer protection

1. Identify **actual** native point of reward settlement relative to entering post-victory garage. Compare `after_reservation` vs `before_reservation` cases at 0 Scrap, genuine win reward 70, two newly destroyed parts, and paid rebuild price 70. Both must preserve exactly one free reserve per authenticated owner epoch and complete at NEXT distinct win only. Do not copy the Python toggle to live authority or assume cash-on-win exists for all challenges.
2. With a dead Core, no next challenge, or essential 0HP and no authorized recovery, a deferred ticket may never finish. Instrument paid reserved Scrap **still outstanding and unreachable** at run end; decide explicitly whether it is safe to permit purchase, cancel before commit, roll over to next run, or refund. This simulator never implements refund, automatic release, teleport or core resurrection. Verify authoritative wallet debit journal and duplicate-event protections before claiming such behavior.
3. Avoid selling future-win conditional repairs without showing when the actual player can realistically win again. Distinguish paid reserved, free reserved, denied for insufficient Scrap, damaged unreserved, and physically completed tickets. Re-enable playable continuation only if B01-00 approves an actual eligible alternative (#154 not yet approved).
4. Check hostile source tests independently against B01-22 V3 #694, V4 read-only #716, canonical world+storage #729/#712/#669. A matching arithmetic test is NOT a transaction safety proof, two service session authority, replay fence, or an observed native gameplay fix.
5. Test real Light/Heavy, lost/won battles, initial empty wallet, timing of reward grants, last battle of a run, no eligible next challenge, dead Core, blocked CanDeploy, single one-shot loaner WIN/LOSS if later approved, plus at least two concurrently opened service instances. Player sessions = **NOT_RUN** now, cost/cadence = hypotheses.

**Gates and evidence:** Original source Python 120 tests and 12 fictional scenarios with cross-OS exact SHA only after actually completed runner checks. Unity 6000.3.14f1 G1, human usability/safety study, integration and Codex feedback remain **NOT_RUN / NOT_MEASURED** until genuine events.

## W24 owner-approved policy — independent implementation acceptance

1. Exactly 1 free destroyed-part rebuild per verified distinct combat WIN, no free reservation before first WIN. No refill by defeat, reload, duplicate event, caller bool, or accidental zero-cost paid ticket. Newly damaged in WIN N waits until WIN N+1; previously accepted delayed rebuild heals only by authentic win.
2. Price for destroyed positive-priced owned part: exact integer-floor 65% of purchase; positive purchase 1 => zero-cost PAID order that preserves WasFree=false, physical GUID, owner ledger and quota. Above-zero HP repair is separate authority. Check order signature and paid/free counts at price 1, 2, 3, 100.
3. Only physically nonfunctional rig is blocked, based on trusted CanDeploy and real owned Core/gun/drive/structure, NOT every destroyed optional part. One weaker temporary loaner per run with living Core and eligible next challenge; no repeat after loss or win. A loaner loss does not heal, duplicate rewards or free quota.
4. SOURCE RED WITNESS P0: with zero initial Scrap, zero prior genuine wins, destroyed essential gun, living Core and next eligible encounter, one loaner can WIN but old V3 heals only previously reserved parts; no pre-win free eligibility or cash means no pending order. Win advances epoch but the gun is only reserved afterward, requiring an impossible second combat after the loaner is exhausted. Owner must resolve with trusted conditional pre-win request or another explicit approved noncheating pathway, and demonstrate separate no-fake-win/old-part-ID/replay/rollback/cold-restart tests.
5. Original V3 source also permits free at epoch0 through FreeUsedAtWin=-1 versus Wins=0, and rejects Purchase=1/PaidQuote=0. Never edit frozen V3; B01-22 publishes NEW version and B01-61/41 independently negative-tests original and correction. Save only local crash/corruption threat model; no relaxation of one physical owner or durable atomic CAS.
6. Actual Unity G1 must prove Light/Heavy, two service instances, 0HP required vs optional, dead Core, missing challenge, loaner WON/LOST and no double award, pending after same vs next WIN, price1, 65%, genuine authorized wallet order. Human sessions remain 0; policy APPROVED is not implementation QA ACCEPTED.

Original source model now has 145 deterministic cases and 15 synthetic fixtures; exact SHA Windows+Ubuntu source CI to be verified. Receivers B01-22/20/13/00, independent G0 B01-40, native G1 B01-60.

## W25/R6: B01-22 V6 quote contract and native blockers

Proposed common whole-Scrap 65 percent: purchase>=1, quote=MIN((purchase*65+50)/100,purchase-1), arithmetic long. Results price1=0 PAID not FREE, price5=3, price15=10, price30=20, price100=65. Requires explicit B01-22/00 owner ACK, then a NEW author-owned V6 version; do not overwrite frozen sources.

Independent B01-10 compiles exact original V6 C# from #764 Git blob under .NET8/C#9 Windows and Ubuntu. Expected negative is source P1 1->1 (does not meet stricter owner requirement); positive original source stage test is first WIN unlocks one free ticket, second DISTINCT WIN only completes previously booked ticket. Independent source negative also proves zero-wallet first one-shot loaner winner may still leave essential gun 0HP with loaner spent, requiring a genuine owner-side source recovery fix. None are native Unity G1 or a real trusted combat receipt. Never count detector GREEN as original V6 implementation accepted.

Preserve same physical ID after repair; no double free, no cross-run-loaner revival, real source CAS under ONE selected .owner Save, collision-safe rounding, external attacker anti-tamper out-of-scope. Source Python160 cases and 17 invented scenarios. B01-22 must implement fixes in new version, B01-00 decide whether one common rule is approved, B01-61/41/40 independently verify actual original source, B01-60 run real Unity G1 if owner-operated. No release, no merge.

## W25 R7 — final owner quote disposition / original C# source acceptance

Canonical B01-22 owner selected V7 PR #780 floor(65% * purchase) with exact quote 1->0 PAID, 5->3, 15->9, 30->19, 100->65. B01-10 proposed half-up-clamp from W25 R6 is superseded. Never use V6 #764 old half-up in production. B01-10 CI must compile original frozen V6 expected source quote RED and new original V7 expected source quote GREEN at their real exact Git SHAs, and validate future WIN N+1 repairs only previously booked 0HP component. Real anti-softlock one-shot loaner 0-Scrap winner still needs B01-20/22 explicit host-safe path. 160 original Python design tests, 17 invented scenarios; independent G0 PENDING, native Unity G1 NOT_RUN, no integrated authoritative Save/physics or merge.
