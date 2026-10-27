# CHL-003 — B01-10 W16 measurable gameplay acceptance review (READ-ONLY SUPPORT)

**Not a third gameplay package.** [Governing issue #276](https://github.com/C8LyNn/RoboRougelike/issues/276) allows exactly PARTSIZE-001 (B01-23) and CHL-003 (B01-20) source production. This branch touches **only B01-10-owned design/QA files** in `Docs/B01Design/CHL003_W16/`, no Assets, runtime adapters, existing CHL producer files, Codex worktree, or protected branch merge.

**Source ownership:** B01-20 builds isolated physical [CHL-003 #91](https://github.com/C8LyNn/RoboRougelike/issues/91); B01-21 route fixtures, B01-30 graybox; B01-60 independent Unity QA; B01-40 artifact gates. #79 Codex inbox is ACK_PENDING for overlapped gameplay, not proof that Codex has accepted CHL. This contribution is external *design verification only*.

## 1. Physically fair objective

One 3–5 checkpoint Rigidbody obstacle course: start -> low barrier -> ramp -> wide/clearable bridge -> checkpoints in order -> finish. `Ready -> Running -> Completed|Failed`, deterministic Retry reset, optional timer disabled by default. Native physics determines actual success; reference geometry alone never declares win.

**Contradiction to eliminate up front:** measured illustrative widths: Light **2.980m**, Heavy **5.180m**. With per-side clearance proposal **0.25m**, theoretical MINIMUM *straight* continuous passage: Light **3.480m**, Heavy **5.680m**. A mandatory 3.70m "narrow bridge" is only adequate for Light, would exclude Heavy despite requirement to compare both on same route. **CHL must either make the mandatory bridge >= 5.68m (plus dynamic swept-turn, suspension and steering validation in Unity), or provide a Heavy-safe bypass that crosses identical REQUIRED checkpoints and permits the exact same completion/scoring.** The physical slope/corners may demand wider than the lower bound. Do not quietly give Heavy a time/reward handicap or call a failed optional ramp a failed entire course.

We have NOT measured actual CHL demo Light/Heavy hull heights/width yet. 2.980/5.180m come from earlier R6 study, not certified dimensions for future B01-20 test rigs. The fixture heights 2.1/3.8m are explicitly synthetic illustrative tests, not measured geometry.

## 2. Exact sensor semantics

- **Plane crossing**: full chassis (not its loose collider) must pass an active ordered gate; detect segment from prior -> next *50Hz simulated physics pose* crossing plane Z forward. This catches movement that skipped a thin trigger between two physics poses but is **geometry only** until PhysX/rig tests.
- **Full width and height**: accepted gate aperture must enclose actual chassis envelope at crossing, not just Core pivot (especially Heavy/tall). Boundaries must use runtime measured collider envelope with swept-body/path when rotating.
- **Identity**: authenticated single runner actor, not any projectile/detached wheel; multiple child colliders may not increment the same checkpoint twice.
- **Sequence**: 3–5 checkpoints, strict order, cannot win by finish-first, wrong order, duplicate trigger, or jumping past a checkpoint. Finish emission once per generation/run.
- **Time**: FixedUpdate canonical 50Hz clock 0.02s per tick, never HUD.Update additive timing; pause freezes simulation elapsed. With opt-in positive limit, reaching deadline exactly is timeout, not a win.
- **Retry**: resets elapsed/checkpoint/progression/pause and increments session generation. Stale callback from previous generation cannot mutate current run or award a second completion.
- **No reward import**: CHL sandbox core never edits canonical inventory, repairs, credits, garage or rewards; any future adapter is separate Codex-owned integration contract.

## 3. Actual portable tests performed

Run `node Docs/B01Design/CHL003_W16/validate.mjs`. It executes **37 source-only** positive/negative cases (10 route widths, 15 checkpoint sensors and 12 state/clock sequences), plus 3 mutation negatives to prove the oracles reject malformed expectations or missing clearance/actor authority. Tests validate the *illustrative acceptance logic*, NOT CHL-003 Unity code, actual PhysX, real vehicle completion or independent QA.

All test input/expected codes in [fixtures.json](./fixtures.json); canonical decisions and unknowns in [contract.json](./contract.json); current reproducible test result in [EVIDENCE.json](./EVIDENCE.json).

## 4. B01-20 narrow producer ask (no source edit by B01-10)

Create a single independent Unity scene with 3–5 actual checkpoint triggers, low barrier, angled ramp and physically collideable bridge. Implement event authority and truly physical drive via one Light and one Heavy different assembled Rigidbody vehicle (not direct API completion). Prove exact [CHL003 #91 native test list](https://github.com/C8LyNn/RoboRougelike/issues/91): E01..06 and P01..06 using real Unity logs/XML source SHA. Add our `WIDTH-03/06/07`, `GATE-02/03/10/11`, `STATE-04/06/08/09` as negative integration cases; verify actual physical passage independently before declaring G1. Before native physical measurement, width values are planning guardrails only.

**Paired playtest design:** equal start/finish and same course geometry for both rigs, three clean traversals each at same seed/conditions; raw fields: rig measured hull (width/length/height), mass and CoM, frame/fixed step, checkpoint index/epoch, fall reasons, wall contact, consumed retries, clear time, traversal success, pause ticks and event count. Do not assert which rig must be faster before observation. Provide video of 720/1080 HUD + external recorder with clear checkpoint counts. The 3 trial comparison is a minimum diagnostic pilot, not proof of statistically robust balance.

## 5. Handoff

Expected native G1: *NOT_RUN*, no CHL producer PR confirmed at audit time. G2 HOLD. Codex integration only after exact frozen CHL SHA and B01-60/B01-40 gates. Rollback this support branch with no effect to game; no new runtime package is being offered.
