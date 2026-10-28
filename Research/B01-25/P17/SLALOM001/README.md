# B01-25-SLALOM-001 / R1 — four-gate slalom authoring arena

**Owner:** B01-25 LEVEL & CHALLENGE DESIGN. **Purpose:** one new *importable original* MapDocument v2 arena with four alternating full-wall passages, not another procedural MAPGEN preset, not B01-20/21 CHL or Codex LD02 implementation. This is a separate authored map for testing actual vehicle maneuvering / reversing / collisions before a Codex receiver decides to integrate. No canonical `Assets/**` mutations.

## Real source assets

| File | What Codex obtains |
|---|---|
| `arena.json` | **360 native MapDocument v2 elements:** 252 Ground, 92 Wall (56 perimeter +36 chicanes), four Platform corner blocks at 4m height, six Pillar, four LowBarrier, Spawn(7,2), Finish(7,15); local floor |
| `preview.svg` | **Actually source-derived** bird's-eye static visualization with centerline and four gate labels |
| `playable-grid-preview.html` | **Interactive no-server offline grid walkthrough**; keyboard/touch stepping, real cell occupancy collision, full G1–G4 score, reset, route visibility. Entire original map and manifest embedded; no external fetch or new gameplay code |
| `manifest.json` | Four gate ranges, immutable original map Git blob, mandatory course geometry and a clear 49-step reference; original C# pins |
| `validate-core.mjs` | Original data/geometry core, source-aligned primitive AABBs, eight perimeter probes, 0.25m sampled reference centerline and route reachability |
| `test.mjs` | Reproducible Node 18+ replay: Git SHA1 content pin, **42 positive source checks**, **7 deliberately corrupted map negatives**, actual embedded original HTML JS test in isolated Node VM with stub DOM+Canvas |
| `evidence.json` | Truthful original execution receipt, immutable blob identifiers, incomplete acceptance gates and remaining risks |

**Not yet in game:** the four gate markers G1–G4 exist only in the manifest/HTML. `MapDraft` has one Spawn and one Finish, NO checkpoint / timed-run / reward logic, and this packet adds none to canonical C#. The browser walkthrough moves one cell per button; it is neither Rigidbody nor vehicle handling nor an AI fight.

## Level design with measurable criteria

- Physical graybox width **56m**, length **72m**, `Grid=4m`, local ground only; walls block alternate rows at **z=4 (east gap), z=7 (west gap), z=10 (east gap), z=13 (west gap)**. Each of the four passages has **three clear grid columns**; all are bordered by existing source `Wall` and edge geometry. The four original thin-wall corner leaks are sealed by `Platform(kind=6,scaleY=4)`, without footprint overlap.
- The reference line avoids obstructed cells and hits all four gate centers: (11,4), (2,7), (11,10), (2,13); **49 tile-to-tile transitions** (not seconds). Original-source AABB grid BFS finds a shorter **41-step path**. This difference intentionally lets Codex evaluate route risk and physical clearance, but does not implement scoring or payouts.
- Source-space geometry checks sample the reference line every **0.25m**, with conservative bounding boxes for `Wall`, `Platform`, `Pillar` and `LowBarrier`. The measured minimum source-box distance is **2.0m**; this is **NOT a vehicle envelope guarantee**. Wheelbase, rotation, suspension, off-axis cornering, true PhysX contacts, slope dynamics and camera occlusion are unmeasured. Real Heavy may still FAIL and should report that as a gameplay finding, not forced PASS.
- Rule for future challenge manager, **only if Codex/HQ approves it**: G1→G2→G3→G4 then Finish; if no checkpoint system exists, use **only Finish** in current MapEditor sandbox and evaluate gate sequence as manual observer notes. **No invented enemy or run reward.**

## Actual reproducible test and acceptance

**Source-only:** from repository root, execute:
```bash
node Research/B01-25/P17/SLALOM001/test.mjs
```
Expected: baseline 42 predicates pass; seven negative mutations (corner gap, blocked gate, duplicate spawn, missing spawn floor, deleted chicane wall, global floor, corrupted reference waypoint) all **RED as intended**; original HTML's embedded map equals `arena.json`, and actual inline JS completes a 49-step four-gate walkthrough inside isolated Node VM. This is **not Chromium or Unity**. `validate-core.mjs` was **actually executed through V8 with identical original GitHub source body**; HTML source run with DOM/Canvas stubs **actually executed through V8**. The standalone Node command is **authored, not executed yet** in this producer session, so don't label Node/Chrome PASS until a receiver runs them.

**Codex editor import (conditional, sandbox only):**
1. Check original SHA of `arena.json` against `manifest.json.mapBlob`. Make a copy of the exact file into a **fresh/disposable** `Application.persistentDataPath/Maps/ARENA17_SLALOM001.json` (do not overwrite user maps, do not let this test exercise live saves). In the existing map editor set `EditorName=ARENA17_SLALOM001`, click **Wczytaj**. No external file picker is assumed.
2. **Native load PASS:** `MapDraft.TryLoad(...)` returns true with `TestError() == null`, 360 objects and above counts, `globalFloor=false`; the generated `MapWorld.Build` correctly creates solid colliders at four outer wall middles and four corner platforms.
3. **Native drive PASS:** run genuine **Light and Heavy** separately from Spawn and physically traverse the four alternating gaps then Finish, recording elapsed time, minimum actual `Collider.bounds` clearance and collision impulses at every chicane, steering input, camera view, any stuck/softlock, any approach into the open map boundary. FAIL if either vehicle cannot finish by regular inputs, overlaps solids or escapes the arena. The 49-step grid route does not mean a real vehicle can follow it at speed.
4. If native gameplay does not have checkpoints, **do not report G1–G4 runtime PASS**; instead record observed gate crossings in test operator notes, `CHECKPOINT_SYSTEM=NOT_IMPLEMENTED`. A manual physical finish or native test should also record current Codex source SHA, original input Git blob, authentic NUnit XML, Editor.log, Player.log and process exit.
5. **Independent B01-60 QA** requires original source SHA reproduction and separate HQ policy for native G1; B01-40 verifies this isolated package. **Do not interrupt Codex current LD02/run-save work**. No game integration/G1/G2/merge without owner signoff.

**Rollback:** if the test copy was placed in disposable `persistentDataPath/Maps`, exit test/editor and delete only `ARENA17_SLALOM001.json` in that temporary profile. Reset/discard the disposable profile, never delete player save files. For future approved Codex integration, remove this test map reference by Codex-owned revert; this package itself touches only `Research/B01-25/P17/SLALOM001/**` and is safe to remove wholesale.

**Dependencies/ownership:** original source pin at authoring: Codex #310 `682dc0c5c13615577334375ffb6c27be5cdebf4a`, `MapDraft.cs` blob `0331a54a1545ecd1ea18b616ef13fed113474b8c`, `MapWorld.cs` blob `8928908db123b35dcab4b324fe9d3a7a8fceee4e`. Game PR moves; receiver must **repin current actual Codex HEAD** before native testing. No third canonical gameplay package; no MAPGEN producer, no CHL/PARTSIZE, no shared CI. B01-25 owns this research branch. Hand off through Draft PR and #330 to B01-40/B01-60, then B01-00 decides Codex #79 sequencing.
