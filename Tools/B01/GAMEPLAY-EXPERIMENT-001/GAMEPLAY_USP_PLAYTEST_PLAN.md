# B01-10 / Gameplay USP — measurable experiment contract R1

**Reference:** [Project market/design hypothesis #250](https://github.com/C8LyNn/RoboRougelike/issues/250). **Role:** B01-10, design/balance, source-only analytics. **Status:** analytic protocol available; **actual players, R3/R6 PhysX and full game playtests NOT_RUN**.

## Design hypothesis (not a proven differentiator)

> Each battle changes the machine; after battle, choosing up to 3 non-armor salvaged items, repair cost, and physical reconstruction should create meaningful tradeoffs between the next challenge and combat risk.

Game feature comparison with TerraTech Legion, Trailmakers, Crossout, Besiege and FUMES in #250 is background, not evidence of gameplay outcomes or popularity. Measure behavior rather than assert uniqueness.

## Test plan: 100 fixed seeds per archetype and activity

**Independent build archetypes** (examples, not mandates): balanced 4-wheel generalist, heavy armored gunner with asymmetric heavy weapon, lightweight fast/weak runner. Each must have verified functional mobility and combat weapon where required. Keep an identical price/cells budget where possible; otherwise report differences and do not draw causal conclusion.

**Activities**: reactor defense / race / armored boss, using the same RNG seeds (0–99) and objective version, enemy difficulty, physics fixedDeltaTime, Rigidbody parameters and controller. Matrix **3 builds × 3 activities × 100 seeds = 900** paired observations minimum for automated API-driven benchmarking. A human playtest supplements automation; it cannot be replaced by the same scripted agent playing 900 times.

**Metrics:** completion rate; time to finish, repair Scrap; first meaningful control/action seconds; garage assembly time; changed parts count after mission; actual functional wheel loss and angular steering response before/after; non-armor loot<=3, armor loot=0; type of next mission; satisfaction and perceived meaningfulness captured through separate human short form. Record player skill, input device, Unity/Windows build SHA and scene seed in full native dataset extensions.

**Gameplay diversity decision:** tool flags a build as a **potential dominant strategy** only if across 3 activities on >=30 *paired identical seeds* per activity it has completion >= comparator and median repair cost <= comparator, with at least one strict advantage. This is a **screening heuristic** (not a proof): confounders include aim accuracy, build cost and skill. If a single build dominates, assign matchup redesign to B01-00 rather than arbitrary nerf without manual playtesting.

**Functional damage criterion (hypothesis to calibrate):** for actual lost wheel events, compare matched steering yaw deg/s before/after, plus traction/roll/drift metrics and driver report. Script flags wheel breakage with <5% steering-response difference as **investigate**; this is not a blanket game FAIL because symmetric redundant drivetrains can legitimately preserve mobility.

**Economy-choice readout:** track post-fight options actually shown, 0–3 non-armor items kept, damage-rebuild cost vs earned Scrap, repeated choices across 2–3 victories and actual next-build modification. Do not convert armor into one of the 3 keep slots; do not credit stolen/destroyed or ineligible physical instances. Add owner-event deduplication and actual per-item UUID from authoritative R3 inventory when the telemetry adapter is built.

**Human UX experiment (P1):** minimum 5 participants per exploratory round (suggested recruitment target, not a statistical power guarantee), two comparable 10–15 minute sessions per player, counterbalanced first-build/activity order; record time-to-first-fun, damage attribution, critical garage warnings, number of successful first run setups, repair understanding, number of meaningful rebuilds, effort-to-restart, satisfaction (1–7 Likert), one open reason. Observe confusing GDD/UI states before claims. Any usability threshold or statistical significance requires later power analysis and HQ signoff.

## Status and handoff

- B01-10: deliver portable analyzer, exact fixtures and fail-closed integrity validation (this PR). No game scene changes.
- B01-60: collect independent real Unity Player session logs/videos and manually review outliers, e.g. duplicate event, actual post-wheel-loss steering, loot and rebuild choices.
- B01-20/23/22 & Codex **after owner scope reservation only**: add minimal R3/R6/R1/ECON read-only event adapter emitting `eventId, buildId, challenge, seed, finished, seconds, repairScrap, lootedNonArmor, lootedArmor, wheelDamage, steeringBeforeDegSec, steeringAfterDegSec, partsChangedAfterRun, garageSeconds, firstMeaningfulActionSeconds`; don't add multiple telemetry owners. Schema in companion JSON.
- B01-00/HQ: select 3 test builds/activities and authorize test captures. No G2, auto-balance or Codex integration from synthetic evidence.

**Do not confuse:** CI output on `SYNTHETIC_FIXTURES.json` verifies parser and mathematical detection only. **`native_player=0`, `native_api=0`, no real balance conclusion**.
