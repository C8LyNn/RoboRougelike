# MAPDIFF-001 R1 — Read-only visual map change preview

**Owner:** B01-62 — DATA INTEGRITY & SAVE QA. **Package:** MAPDIFF-001/R1. **Status:** independent functional map authoring tool; NOT a canonical Unity/game integration.

## New functionality

The standalone Python mapdiff.py compares **two MapDocument v1/v2 JSON map versions**, creating both a structured mapdiff.json and a standalone mapdiff.svg with BEFORE / AFTER / CHANGED CELLS panels. It respects original Codex MapDraft.Footprint rotation and scaleX/scaleZ semantics, separate ground/object layers, v1 migration (scale=1), Spawn/Finish uniqueness and per-layer item limits.

It finds added/removed items, occupancy replacements, per-cell footprint changes, and globalFloor switches. **Inputs are never modified.** Reports include original byte SHA-256 and normalized semantic SHA-256. Unchanged geometry with reordered JSON has the same semantic digest even if file bytes differ.

This provides an actual visual comparison before the operator chooses a backup for recovery using MAPSAFE-001 (prior separate PR #347). It does NOT restore maps, mutate the game or alter the run-save system.

## Use

Python 3.11+ standard library; no pip, Unity activation, network or remote service. Create an output directory separate from input map parent folders:

    mkdir comparison
    python Tools/B01/B01-62/MapDiff001/mapdiff.py --before LD02_ARENA.json --after LD02_COVER_CHOICE.json --out-dir comparison

Generated files:
- comparison/mapdiff.svg — standalone scalable picture with before, after and highlighted changed cells, static item tooltips.
- comparison/mapdiff.json — deterministic machine-readable object/cell delta receipt with semantic and byte-level digests.

Open SVG in a browser or vector editor. Different parts preserve original MapItemKind numeric codes Ground=0, Wall=1, Ramp=2, Obstacle=3, Spawn=4, Finish=5, Platform=6, Pillar=7, LowBarrier=8, Steps=9. This is **not** Unity PhysX, a NavMesh or a runnable gameplay map scene.

## Actual reproducible tests

    python -m unittest discover -s Tools/B01/B01-62/MapDiff001 -p test_mapdiff.py -v

Scoped GitHub Actions executes the same Python source on Windows and Ubuntu, then fetches **two original immutable Codex #310 maps** pinned to commit a2ccd055070a963805cba0779b79f18489f4794f, with Git blob identity checked:
- Docs/CX08/Fixtures/LD02_ARENA.json — e39e2f174984832bd97311c050e3c22fd6fb1175 — 168 items
- Docs/CX08/Fixtures/LD02_COVER_CHOICE.json — 8586312b9606d94b088e4e7640c28105e2d6bac1 — 173 items

A source-derived diff must identify **11 added items, 6 removed items, 162 unchanged items**, including five new walls at coordinates (4,4) through (8,4). Real acceptance asserts the original JSON byte hashes, geometry, parseable SVG and outputs as actual downloadable CI artifacts.

## Install, dependencies and rollback

Owned paths: Tools/B01/B01-62/MapDiff001/** and .github/workflows/b01-62-mapdiff-001.yml only. There are **zero changes** to Codex game Assets/**, in-game MapStorage/MapDraft, run-save, economy, CHL, PARTSIZE, old MAPSAFE-001 or other owners.

Codex may copy this complete tool folder to an offline development workspace and run Python directly. No Unity import, .meta GUIDs or game API change needed. Rollback: delete ONLY this tool folder and its workflow; keep all original maps, user backups/Vault and any exported comparisons. B01-40 reviews source provenance; B01-60 independently checks QA; native Unity G1 NOT_RUN and G2/CODEX_ACK remains pending.

## Known limits

- This parser is intentionally strict and does not implement Unity JsonUtility or accept custom JSON schema extensions.
- Object identity compares normalized values; moving a part is shown as one removed and one added item rather than guessed continuity.
- Global floor switches are reported in JSON but not drawn as an unlimited surface.
- Cannot guarantee safety during concurrent gameplay map modifications. Compare offline copies after closing the editor.
- MAPSAFE snapshot manifest verification is NOT performed by this tool: first run MAPSAFE-001 verify, then compare the payload.json inputs.
- This is a static geometry reference, not a physical clearance/route test or direct game feature integration.
