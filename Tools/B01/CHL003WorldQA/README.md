# B01-21 supporting CHL-003 R1: real MAPGEN source-backed terrain/seed QA

**NOT a third gameplay package**. This directory provides **external read-only QA inputs** for the ONE B01-20-owned CHL-003 R1 (#91), following HQ exclusive-owner governance #276. Absolutely no CHL runtime/scene, MAPGEN/MAPGENWORLD producer, Codex current game, camera, garage, economy, PARTSIZE or Unity asset files edited.

## Provenance and exact boundary
Runs unchanged actual MAPGEN R3.4.1 [producer PR #237](https://github.com/C8LyNn/RoboRougelike/pull/237), pinned commit `5be31039411f89f17654b1c332f8692883fa03cc`, **real C# MapGenPlanner/MapGenRouteValidator/MapGenData** against their preexisting explicit surrogate UnityEngine math (`Tools/B01/MAPGEN001/Headless/UnitySurrogate.cs`). This demonstrates deterministic source geometry/route preflight, not Unity collider, Rigidbody, wheel, real vehicle, CHL checkpoint state machine, or native gameplay success. The genuine CHL-003 physics implementation does NOT yet exist at this source baseline.

## Executable cases
- **18 distinct source route baselines:** seed7/31/83 × 3/4/5 ordered checkpoint course segments × measured Light width2.980m / Heavy width5.180m with original rig lengths, turn radii, 0.25m clearance per side, default width16m. Each runs the original planner+route validator twice and captures original geometry hash, start/finish and all waypoint coordinates in `CHL003_world_source.json`. These are **candidate reproducible references** only, not a promise about CHL geometry.
- **10 fail-closed narrow source cases:** seeds2/12/14/22/28 × measured light/heavy, lane10m must reject `IMPOSSIBLE_COURSE_PROFILE` rather than use nominal 2.1/4.5m.
- **1 real-source obstacle-injection negative:** block first checkpoint corridor with an actual `MapElement`, confirm route validator rejects physical footprint `SPINE_FOOTPRINT_BLOCKED`, and recomputed geometry hash differs from saved one.
- **1 ordered-checkpoint scenario precondition:** shuffled [0,2,1,3] after first valid hit, expected prefix 1. **The CHL controller is NOT exercised.**
- **20 actual Python independent JSON validation tests**, including mutated/missing seed, omitted narrow negative, forged route pass, wrong real hull/margin, fake Unity success, tampered waypoint/finish, invalid hashes. These do not replace independent B01-60 physics QA.

## Reproduce from pinned isolated source stack
```bash
dotnet run --project Tools/B01/CHL003WorldQA/CHL003RouteQA.csproj -c Release -- /tmp/CHL003_world_source.json
python3 Tools/B01/CHL003WorldQA/verify_receipt.py /tmp/CHL003_world_source.json
CHL003_RECEIPT=/tmp/CHL003_world_source.json python3 -m unittest discover -s Tools/B01/CHL003WorldQA -p 'test_*.py' -v
```
CI confirms actual producer Git blob hashes prior to running source tests; logs, JSON, SHA256SUMS and tested HEAD are in CI artifact. No `dotnet obj/bin` directories shipped as source. Tests are expected to execute Linux and Windows (source QA), and neither environment implies native Unity.

## RED->FIX compatibility finding from original immutable MAPGEN data
Initial real GitHub CI on source QA HEAD `3125f8fbbc4ce88601076ea11d91efde04aa952a` produced **18/30 FAILED**: the new QA harness assumed last checkpoint physically precedes finish. Actual frozen MAPGEN returns its final checkpoint **COLOCATED with Finish** (identical world XYZ) on all 18 source scenarios. This is a **test assumption failure and an important CHL source handoff constraint**, not evidence of a broken Unity activity. Corrected test now asserts exact colocation and requires a separate `receiverMustDisambiguateFinishTrigger=true` flag, verified independently by Python mutation tests.

**CHL-owner decision:** if reusing MAPGEN checkpoints, either author a finish trigger outside the last checkpoint collider or ensure activity state-machine processes checkpoint N before Finish, even if Unity fires overlapping `OnTriggerEnter` callbacks in either order. An early finish must not complete; crossing last CP and finish at same fixed frame must yield exactly one lawful completion, no skipped checkpoint. Preserve this as a required native negative regression, **not a runtime fix by B01-21**.

## B01-20 CHL author handoff (no forced generator import)
Use deterministic seeds 7/31/83 and identical physical Light2.980/Heavy5.180 envelopes, 3/4/5 **ordered** checkpoints, fall/Retry/Pause, source route data as a reference. A CHL-authored scene **may have different geometry**: to compare it, explicitly report independent CHL scene geometry hash, actual point coordinates, physical supported corridor width, slope/ground contacts and original Unity NUnit XML. Do NOT simply copy this source MAPGEN hash into a new CHL scene or claim identical map. Real CHL measured route requires Unity Physics (collider sweeps and supported wheels), Rigidbody nonteleport traversal, 3 Retry cycles and both rigs. Check out-of-order, early finish, multi-collider duplicate, wrong actor, fall reset and paused time separately. [Required CHL acceptance issue #91](https://github.com/C8LyNn/RoboRougelike/issues/91).

**Integration and rollback:** no gameplay installation. To remove tool, delete `Tools/B01/CHL003WorldQA/**` and its dedicated CI workflow only. Original producer and CHL never mutated. No Codex handoff/OFFERED from this QA-only result. G1 native independent NOT_RUN; G2 HOLD.
