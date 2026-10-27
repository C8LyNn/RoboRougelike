# B01-25 W58–W61 / PASS 11 — research fixtures (NOT Unity gameplay)

**Exact source:** Codex [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`.

**Usage:** `node Research/B01-25/P11/acceptance-suite.mjs` (Node.js 18+). Each `LD*.json` is a proposed MapDocument v2 shape with numeric MapItemKind IDs 0..9. Do NOT import into an existing user save name: use an isolated/disposable sandbox.

**What is tested:** independent JS implementation of input-validation and footprint invariants explicitly grounded in original `MapDraft.cs` Git blob `b5f8f395f93972c0ede000701dbbada8fcfcdfdb`, plus authoring-level 4-neighbour route witnesses, negative mutations, scenario outcome contract and an ordered handoff matrix. The tests run as original JavaScript, **not** as original Unity C#, PhysX, Windows Player or human playtest.

| Scenario | Geometry-only entry | Runtime requirement |
|---|---|---|
| LD01 Kalibracja napędu | 52 tiles; 2 pillars, barrier, ramp, steps; start and finish; static detour | Real Light/Heavy 3+3 trials; wheel contact/colliders; HQ route policy |
| LD02 Krzyżowy ostrzał | full floor, 4 pillars, 2 barriers, 2 walls, start/finish | No enemy spawn/kill/win condition in editor; Codex/HQ future work |
| LD03 Skrót albo objazd | local floor, two internally separate planned route witnesses; safe detour; risky short barrier | Verify physical clearance for both rigs; no reward implementation; HQ route policy |

**Validation interpretation:** `MapDraft.TestError()` only gates Spawn, Finish, floor under Spawn (when GlobalFloor off). A mere finish trigger can be activated after teleport, so it does **not** prove a route. GlobalFloor draws a full 101x101-cell floor rather than a bounded 12x10 combat arena. Timeout/stuck/kill/reward conditions are **DESIGN_ONLY**.

**No additional source production stream:** isolated `Research/B01-25/P11/**` only; no `Assets/**`, tests/CI settings, third Unity package, Codex files, or merge.
