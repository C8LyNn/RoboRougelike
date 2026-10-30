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
