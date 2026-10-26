# B01-10 — RUN-SAFETY S04 DESIGN QA-001 (source only)

Task [#155](https://github.com/C8LyNn/RoboRougelike/issues/155), parent [#152](https://github.com/C8LyNn/RoboRougelike/issues/152), ECON source [#136](https://github.com/C8LyNn/RoboRougelike/issues/136). Reviewed immutable producer SHA `b2623f50e2bdb16106b88d0107d215867ed8791b` from [Draft PR #154](https://github.com/C8LyNn/RoboRougelike/pull/154).

**Execute from this directory:** `node run.mjs` (Node ≥20, no npm / dependencies). Portable model and JSON fixtures, not Unity native C# or production gameplay. On success returns JSON PASS; on fail exit=1. CI executes the same test, with G2 always HOLD.

Files:
- `RUNSAFETY_S04_DESIGN_REVIEW.md` — quantified game design findings, variants A/B/C decision matrix and exactly which HQ decisions are not yet approved.
- `contract.json` — executable proposed policy options, hard invariants and telemetry.
- `fixtures.json` — 48 positive/negative scenario inputs, expected codes and counters.
- `policy.mjs` — source-equivalent old gate for reproducing **specific logical omissions** plus an independent additional proposed eligibility gate and ECON model. Never claim source C# execution.
- `run.mjs` — 48 fixture runner, 3 mutation controls and 2,000 seeded ECON consistency trials.
- `EVIDENCE.json` — actual locally executed Node result (CI will rerun).

No `Assets/**` or Unity package modifications. No auto-approved temporary loaner, no Codex integration, no G2. Producer may implement after HQ policy choice, B01-20 ownership assignment, and B01-60 independent G1.
