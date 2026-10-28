# B01-40 PASS17 — real original MAPGEN arena production handoff

**PACKAGE:** `B01-40-P17-MAPGEN-ARENA-RECEIVER-001` R1.
**Owner:** B01-40 (integration/build receiver), **producer B01-20** original immutable [#326](https://github.com/C8LyNn/RoboRougelike/pull/326) `66c8988aa06a63036c6abede4c7181c0c7bfd904`, original frozen MAPGEN R3.4.1 `5be31039411f89f17654b1c332f8692883fa03cc`.
**This package contains NEWLY STAGED GAME LEVEL DATA and executable original-source build tooling, not a new runtime generator or a Unity scene.**

## How to execute (Python 3.11+ / dotnet 8 / Git)

Start in a clean checkout of this B01-40 Draft branch, not in a Codex worktree:

```bash
git fetch --no-tags --depth=1 origin 66c8988aa06a63036c6abede4c7181c0c7bfd904
git fetch --no-tags --depth=1 origin 5be31039411f89f17654b1c332f8692883fa03cc
python Tools/B01Validation/P17/receive_mapgen_arenas.py --repo . --out "/tmp/B01-40-P17-new-output"
B01_40_P17_OUTPUT="/tmp/B01-40-P17-new-output" python Tools/B01Validation/P17/test_p17_package.py -v
```

Use a **nonexistent external output directory** (example above is Linux; choose the actual Windows temporary folder on Windows). Tool refuses overwrite and rejects a target inside the main checkout. It creates a **temporary detached Git worktree**, pins actual owner and original generator Git objects, compiles and executes **unmodified original** C# using .NET8, independently checks 18 unique exported layouts and two Light/Heavy route receipts per seed, runs the producer's original JSON and SVG validators, packages 41 portable fixed-metadata ZIP members, then removes only its own disposable worktree. The main checkout is never modified. Test fixture mutation failures are clearly labelled; there is no real Unity host or Editor invocation.

## What Codex receives

`B01-40-P17-MAPGEN-ARENAS-CODEX.zip` and `receiver-receipt.json`. In the ZIP:

- `blueprints/*.json` — **18 original MapGenPlanner-generated** diagnostic geometries (3 configurations × 6 seeds), canonical JSON serialization (same values, stable bytes across OS).
- `previews/*.svg` — 18 true geometry-based 2D top-down previews; `gallery.html` — inspectable offline gallery.
- `presets.json`, `results.json` — unmodified semantic input and source results, normalized canonical JSON.
- `codex-generate-calls.json` — actual 18 parameter sets for **existing** `MapGenPlanner.Plan(MapRecipe.CombatArena, seed, config, profile)` / `MapGenBuilder.Generate` (real runtime must implement own host approval), with original generated geometry hash as comparison target.
- `receipt.json` — exact original Git SHA and **40** per-member hashes, operator boundary `Unity=NOT_RUN`.

**Do not import `MapBlueprint` as `MapDocument v2`.** These are different schemas. The packet tells Codex which **existing MAPGEN recipe/config/seed** to use, not how to replace its generator or game loop.

## What is actually exercised

CI builds the same 18 layouts from original pinned C# on **both Windows 2025 and Ubuntu 24.04**, passes 36 original Light/Heavy planner/route checks, six original invalid inputs, 18 author blueprint readbacks, 18 SVG parse checks, then runs 16 independent negative/positive physical ZIP tests and publishes a real handoff artifact. Portability requires identical SHA256 for the **inner ZIP** across both operating systems; compare actual two Actions jobs, don't assume it. A source-level / .NET result is not a Unity PhysX or human-playtest result.

## Codex intake / QA / rollback

After acknowledgement of exact package SHA through [CODEX INBOX #79](https://github.com/C8LyNn/RoboRougelike/issues/79), Codex may use the parameter data in an authorized real Unity build with compatible MAPGEN source. Before declaring integration: actual Unity 6000.3.14f1 compile, three seeds **per variant**, two real vehicle rigs, physical ground, spawn clearance, cover collision/LOS, traversal, victory, reward, reset/restart, Editor test XML/exit and Windows Player trace. Independent B01-60 G1 and HQ-only G2 still required. Do not independently cherry-pick or merge these data into main/develop.

To **roll back** the source-only delivery, discard the external ZIP and associated receipt; it writes **nothing to the real Unity project**. For native integration rollback, Codex exclusively owns its source changes, separate worktree, backup and trace. We do not claim an already tested Unity integration, ACK, G1, or G2.
