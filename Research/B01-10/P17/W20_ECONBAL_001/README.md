# B01-10 — ECONBAL-001/R1 — rebuild cadence design model (PASS17 WAVE20)

**Author B01-10, game design and balance; NOT runtime rebuild authority.** Parent HQ [#668](https://github.com/C8LyNn/RoboRougelike/issues/668) and ECON B01-22 [#252](https://github.com/C8LyNn/RoboRougelike/pull/252), [#620](https://github.com/C8LyNn/RoboRougelike/pull/620) independent SOURCE RED, and NEW B01-22 [#673](https://github.com/C8LyNn/RoboRougelike/pull/673) atomic owner implementation. #673 source QA green on a surrogate disk owner is **not live R3 game authority, not native Unity, and not released**. This B01-10 package DOES NOT patch that implementation, frozen #252, #510, #556, #596, chosen R4 #647, rival ALT #652 or Codex.

## A concrete balance decision, not a fake game implementation

The current owner hypothesis is **one global free rebuild reservation per authenticated victory** and paid subsequent reservations. One *purchased* quote is a tuning variable; current original ECON #252 says 65–75% of purchase price is **a hypothesis, not final tuning**. Exact `100 Scrap purchase / 70 Scrap discounted rebuild` is hypothetical and makes comparisons auditable. Includes the original #252 epoch-zero bootstrap (initial single free coupon) **and** a zero-bootstrap alternate to reveal that policy's effect. The alternative `per_run` uses only one coupon for the entire run. Neither alternative is authorized for production based on these calculations alone.

`rebuild_balance.py` is an original deterministic Python 3.12 pure design simulator. It keeps one simulated wallet, one single-owner coupon for the entire run/epoch, ordered distinct attempt/victory identities, unique physical part IDs, reservations paid **before** the event reward, and restores pending reservations **only** after a modeled distinct successful event. Failed challenges do not reset free quota and cannot auto-heal. Parts are prioritized **mobility → weapon → other** for scarce coupons. First epoch bootstrap is optional. Cost is the integer truncated percentage (`purchase * quote_percent // 100`, must be 1..purchase-1). This is **not a physical CAS ledger**, cannot authenticate a real victory, cannot prove a real softlock, and cannot read from/save to any Unity host.

The independent `test_rebuild_balance.py` has **45 authored original executable tests** for the two policies, no/with bootstrap, free vs quoted paid, wins and losses, mobility prioritization under zero Scrap, no mid-event reward credit, duplicate victory ID fail-closed **within the model**, same physical part not billed twice, malformed parameters, deterministic seeds, and conservation. The *actual runtime anti-replay and two-service safety* belong exclusively to B01-22/B01-62 and do not inherit these tests' evidence.

## Original exact design workload (not measured human data)

`scenarios.json` has **6 explicit fictional scenario trajectories**: twelve wins / two destroyed each; six losses/six wins; twelve straight losses/no auto-recovery; zero Scrap and a destroyed mobility part; reward-starved hard encounters; and queued parts completed by a later win. Each plan is evaluated under both cadence policies AND both bootstrap configurations (24 theoretical comparisons). `--stress` generates 200 reproducible, **purely SYNTHETIC** 12-attempt runs seeded 20261030 at modeled 55% win probability with bounded independent part-loss, reward and role selection. It is sensitivity analysis only, **never game telemetry or real player balance**.

**Unambiguous baseline:** With 12 distinct modeled wins and 2 distinct destroyed parts per win, initial 4000 Scrap and no earnings, at purchase 100 / quote 70: per-win bootstrap 12 free + 12 paid = **840 Scrap**; per-run bootstrap 1 free + 23 paid = **1610 Scrap**; delta **770 Scrap**. With no initial free epoch, per-win becomes 11 free+13 paid = **910 Scrap**; per-run becomes 1 free+23 paid = **1610 Scrap**, assuming the first win creates the only per-run coupon later. These are arithmetic on **created example inputs**, not player-observed values. Earlier #673's example is consistent; this package adds risk/price/bootstrapping sensitivity rather than duplicating the owner authority.

## Reproduction

```bash
python -m unittest discover -s Research/B01-10/P17/W20_ECONBAL_001 -p test_rebuild_balance.py -v
python Research/B01-10/P17/W20_ECONBAL_001/rebuild_balance.py --scenarios Research/B01-10/P17/W20_ECONBAL_001/scenarios.json
python Research/B01-10/P17/W20_ECONBAL_001/rebuild_balance.py --stress
```

The dedicated workflow `.github/workflows/b01-10-p17w20-econbalance.yml` independently runs exact original code/tests with Python 3.12 on Windows 2025 and Ubuntu 24.04, saves original execution logs and plainly marked synthetic JSON. Its manifest pins own source bytes, exact base and exclusive Git path scope. **CI or a design model is not native Unity/PhysX G1**; no Unity license is required or consumed here.

## Owner boundary, install, rollback and receiver

No Unity installable assets, .meta GUIDs, asmdefs, native physics dependencies or actual gameplay changes in this PR. **INSTALL:** Run this *offline* with checked-in Python from an independently verified Git SHA; B01-10 must not install files into `Assets/**`. **ROLLBACK:** check out the previous original Git HEAD or close the standalone Draft PR; no shared path deletions or game state changes. Input JSON is only read; result JSON is emitted to stdout and CI `RUNNER_TEMP`. No Unity data cleanup.

**B01-22:** review design math and cost/quote semantics and map to the new, versioned V2 `RebuildEconomyAuthorityV2`, but only B01-22 can modify its runtime code. **B01-13:** accessible refund/critical-risk display and real later UX. **B01-60:** independent C# runtime/native economy QA, NOT implied by this Python model. **B01-40:** source QA/G0 and owner routing. **B01-00:** owner approval of free cadence and eventual handoff. User playtest: see [ACCEPTANCE.md](ACCEPTANCE.md). Codex integration feedback **NOT_MEASURED** until a real event.

Six stages: IMPLEMENTED once source committed; TESTED only after actual exact-SHA test log; QA_ACCEPTED only independently; READY_FOR_INTEGRATOR NO until owner approves and runtime acceptance; INTEGRATED NO; RELEASED NO; native Unity G1 NOT_RUN, playable economy NOT_WIRED, CODEX_ACK NONE, G2 HOLD, no merge.
