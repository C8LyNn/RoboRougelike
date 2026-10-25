# MAPGEN-DIAG-001 — actually executed independent source diagnostics

## Immutable MAPGEN producer pin
- Full stacked PRs #99 + #103 + #211; generator commit `003bced54480d509ffc0d49d26daf87160a8d609`.
- GitHub Actions verified exact untouched Git blobs of MapGenPlanner, MapGenRouteValidator, MapGenData and PlayMode traversal test against producer commit. No generator, Unity test, .meta or game source files changed.

## Executed .NET 8 headless diagnostics
- First executed job: **SUCCESS**, [GitHub Actions run 40186564053](https://github.com/C8LyNn/RoboRougelike/actions/runs/40186564053), job 121148477491, branch-head at execution `a7f9b4a64a09fec0f7ed51f86b6c666cfc5a0d8f`.
- **27/27 independent diagnostic rows PASS, 0 unexpected failures**. Includes 2 intentionally invalid pit variants returning their respective expected rejection codes, 12 base source geometry (seeds 7/31/83 × 2 recipes × 2 profiles), 3 Heavy normal seeds, 4 Heavy hard seeds, 5 Light narrow 10m seeds, 1 PlayMode test-structure diagnostic.
- Actual log reports `PIT/ORIGINAL_START_EQUALS_FINISH=START_BLOCKED`, `PIT/SUPPORTED_ENDPOINTS_CENTRE_HOLE=SPINE_FOOTPRINT_BLOCKED`. For all 24 per-seed cases: source `BFS_SPINE_CLEARANCE_OK`, positive initial waypoint's Z projection; course adjacent floor segments have 1m axial overlap. 12 basic cases are **NOT actual drives**.
- [Artifact 12595589268](https://github.com/C8LyNn/RoboRougelike/actions/runs/40186564053/artifacts/12595589268): 31 files uploaded, including `results.json`, `cases.md`, one JSON per test row, console log and branch SHA; archive size 23,258 bytes, Actions-reported artifact SHA256 `54699d10f3630c5f32fc47dc4e7cb988c9656879c674c2297b53dce3c85f10b3`. This archive digest is Actions-reported, not independently downloaded/rehashed by B01-21.
- This executed output is frozen evidence for the specified **first** diagnostic commit. Any subsequent diagnostic edit needs a new, explicitly verified CI run before claiming that edit passed.

## Read-only native Unity evidence (Codex, not executed by B01-21)
- QA commit `6358dde`, `Docs/MAPGEN001NativeQA/OriginalUnity`: actual Unity 6000.3.14f1 compile PASS; EditMode 22/23 (exit 2), PlayMode 5/6 (exit 2), first **cube** Light/CombatArena seed7 fails waypoint0 after 900 fixed steps, 11 further cube runs not executed. Codex QA is not independent B01-60 approval.

## Gating
- Headless original-source C# surrogate QA: **EXECUTED PASS**.
- B01-21 licensed Unity native compilation/tests: **NOT_RUN**.
- Grounded R6 real wheels/PR125+OBJ210 PhysX driving: **NOT_RUN** in this tool.
- B01-60 native independent G1 **PENDING**, B01-40 G0 target **PENDING**, B01-00 G2 **HOLD**; no merge/integration.
