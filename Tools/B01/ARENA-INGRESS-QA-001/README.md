# ARENA-INGRESS-QA-001 / B01-60 / PASS17 R1

Independent **game-content intake QA**, not a third source implementation package. Read-only validation of THREE **original B01-21 generated MAPGEN001 CombatArena blueprints**, pinned to one immutable source commit and exact Git blobs. Designed to protect the next Codex arena import from malformed or physically blocked data and surface an unresolved real generator spawn-vs-finish ambiguity. **No changes** to canonical `Assets/**`, generator or B01-21 files.

## Run on Windows/Linux with Git and Python >=3.11

From a normal checked-out repository root:

```sh
python Tools/B01/ARENA-INGRESS-QA-001/arena_ingress_gate.py --output ./arena-ingress-receipt.json
```

The tool `git fetch`es the exact original producer commit `a71eda99a260f807ae8cfea71348e75ff4f6c65a` and original MAPGEN planner commit `589b60a912096d1e9917cb0bbac73a53e7543b4f`, verifies exact Git blob bytes of each original blueprint and producer manifest and original C# planner, then runs an **independent conservative 2D 1m four-neighbor BFS** against actual static `Cover` and `Boundary` footprints. The conservative radius fully encloses historically measured Light 2.98×3.20 and Heavy 5.18×5.80 vehicle rectangles, with 0.25m per-side clearance (this can yield false blocked for a physically steerable vehicle; it is not native PhysX).

Acceptance requires three truly distinct original geometry hashes; 1 complete 64m floor and four 66m boundaries; 6 / 11 / 16 original cover placements; sealed external border; finite positive AABB sizes and zero pitch; no cover intersections; no occupant descriptors inside cover; both historical Light+Heavy start→finish grid routes for each map; and **six malformed in-memory map mutations per map must all be DENIED**. Mutations never touch producer data, Codex game or Git history. A machine-readable JSON receipt lists actual paths/route lengths, observed obstacle/cover counts, and separate spawn-point safety warnings.

## Original game data and deliberate non-overstatement

| Original B01-21 blueprint | Seed | Covers | Source SHA |
|---|---:|---:|---|
| `AR16_01_OPEN_FLANK` | 160101 | 6 | `a71eda99...` |
| `AR16_02_SPLIT_DECISION` | 160202 | 11 | `a71eda99...` |
| `AR16_03_DENSE_PRESSURE` | 160303 | 16 | `a71eda99...` |

**Owner original planner observation:** original unchanged `MapGenPlanner.cs` sets `Finish=(0,1.25,25)` AND an enemy spawn *descriptor* at `(0,1.25,25)`. As geometry descriptors, this can be valid if the game resolves the encounter before enabling the finish; it becomes a real block only if current Codex game semantics forbid it or the enemy collider interferes with arrival. Accordingly the report does **not** call this a proven engine bug. It records `HQ_DECISION_REQUIRED_FINISH_SPAWN`; the strict finish-exclusive policy check is expected HOLD for these three originals. An **in-memory, non-production** relocated-spawn positive proves the strict validator is usable. **Do not silently edit or import that synthetic copy.** The initial source-only data path outcome may be GREEN while deployment remains `HOLD_HQ_POLICY_AND_NATIVE_UNITY`.

## Codex receiver/import instruction

1. B01-40 checks this independent test, pinned original owner data and manifest, and confirms an appropriate safe staging boundary. **No import by B01-60.**
2. Codex decides how to ingest B01-21's blueprint schema, which the original generator does NOT currently expose as a canonical map-document importer. No automatic `Assets/**` changes are performed here.
3. Codex/HQ explicitly resolve whether an enemy spawn descriptor and finish marker may overlap **in gameplay**, then Codex runs the actual current integrated scene with Light/Heavy assembled rigs, active enemy AI, kill/reward/exit lifecycle, and real obstacles in Unity 6000.3.14f1.
4. Independent original Unity G1 by B01-60 requires separate licensed host approval. This receipt does not mean G1, G2, build included, or gameplay tested.

**Dependencies:** Git, Python 3.11+, immutable owner/source commits, historical envelopes. **Known limits:** no real assembled robot measurements, no vehicle steering, acceleration or turning-radius physics, no AI spawn execution, no player usability measurement or Unity scene import; conservative AABB/circle may overapproximate blockers.

**Rollback:** simply revert the two independent `Tools/B01/ARENA-INGRESS-QA-001/` files and one `.github/workflows/b01-60-p17-arena-ingress.yml`; no original map files, player save folder, global Editor workflows or gameplay implementation are touched.

**Receiver classification:** `SOURCE_DATA_STATIC_QA` on original map and original compiler hashes, `NATIVE_UNITY=NOT_RUN`, `GAME_INTEGRATION=NOT_DONE`, `HQ_POLICY=HOLD`.
