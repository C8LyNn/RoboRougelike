# B01-21 / W13 — deterministic seed replay and geometry contradiction guard

Status: **standalone opt-in source for independent QA**, no host integration, no G2. Intended package: MAPGENWORLD001 **v1.1.0**, stacked on v1.0.1 [Draft #243](https://github.com/C8LyNn/RoboRougelike/pull/243), upstream frozen MAPGEN R3.4.1 #237 `5be31039411f89f17654b1c332f8692883fa03cc`. User's experimental unified RC2 #258 already integrates **old** MAPGENWORLD001 v1.0.1; do not edit that live candidate in this task.

## Problem and actual correction
A save file preserving only recipe/seed could silently regenerate a different layout if generator logic/configuration/rig measured envelope changed between game releases. The existing adapter's `TryGenerate(request)` deliberately trusts the caller's request and cannot determine whether it reproduces the same world.

**New additive contract**:
- Every successful `MapWorldSnapshot` contains `ReplayReceipt`: format `MW001R1|...` with the exact **recipe/seed, 4 generation config fields, 4 measured vehicle profile fields, required clearance, 64-char geometry hash and upstream generation version**. The payload is invariant-culture with round-trip floating-point precision plus a SHA256 accidental-corruption checksum. **Not cryptographic authentication**: without a server-held secret it cannot prevent a malicious user from recomputing a digest.
- `MapGenWorldAdapter.TryReplay(receipt, out snapshot, out reason)` validates schema, bounded length, checksum, numeric constraints and measured profile using the **actual** pinned `MapGenPlanner.Plan` via `MapWorldPolicy.TryPlan`; only when generation-version and geometry-hash equal saved values will it call existing `TryGenerate`. Refuses mismatch **before producer builder is called**. Does not create another save, restart, inventory, loot or race state authority.
- Error statuses added by appending enum values: `InvalidReplayReceipt` (bad encoding/checksum/version), `ReplayGeometryMismatch` (valid receipt but changed generation version/hash). Existing error enum ordinals are unchanged. Impossible rebuilt rig/config follows original `RejectedGeneratorConfig`/route failure.
- Successfully replayed identical configuration reissues the same byte-identical receipt and new live world revision; normal existing clear/ready callbacks occur. Bad receipt leaves live geometry and callbacks untouched. No automatic alternative-seed reroll: this would violate deterministic restore intent.
- Replay guarantees metadata equality only. Native driving, swept turning, obstacles, ramp accessibility, URP rendering and actual R6 part geometry remain independent Unity/PhysX QA; a geometry hash is not physical traversability.

## Test matrix (actual C# source suite)
Existing 16 synthetic adapter lifecycle cases retained. 12 added: 3 positive (Arena7 Light, Course31 real Heavy5.180m, Polish decimal-culture Course83); 6 independently corrupted input formats (null, empty, unknown prefix, changed checksum, truncated, oversized); 3 signed-but-invalid variants (generation version drift, geometry hash drift, zero actual measured width). The latter three deliberately recompute SHA256 in the **test only**, proving a valid digest alone is not a route guarantee. For every rejection assert no `MapGenBuilder.Generate` call, no loss of previous map and no callback. Exact seed/reason included in case ID.

Native authored tests: 1 new EditMode malformed replay receipt and 1 new PlayMode course round-trip + negative corrupt receipt. Existing Unity tests preserved. Unity test execution = **NOT_RUN** until licensed Unity 6000.3.14f1 runner executes them; surrogate C# cannot establish G1.

## Concrete disposable Unity receiver QA request
1. Import complete frozen #237 producer into a new clean and verified Unity6/URP disposable checkout; install only the new v1.1.0 MAPGENWORLD001 adapter after B01-40 G0 source/target scope check. Preserve existing shared `Assets/B01Packages.meta`.
2. Compile no warnings introduced; run **EditMode 7/7** and **PlayMode 5/5** authored tests. Confirm translated/rotated host, no accidental map destruction on corruption and saved geometry hash roundtrip. Capture actual original NUnit XML and Editor log/exit.
3. Using actual measured Light2.980m and Heavy5.180m, run Arena7/Course31/Course83 with replays and real canonical R6 steering in a separate minimal Windows Player; compare before/after original hash + geometry and wheel paths; historical protected main corridor, **not** optional Heavy side ramp. Light measured10m profile remains expected **REJECT**; if it starts due stale nominal width flag, fail.
4. Check receipt corrupted/altered or generated with different plan version in the live player returns errors and does not clear preexisting map or erase active garage/inventory state. Obtain Windows logs/exit/screenshots/telemetry. Human PC1/PC2 and FPS remain separate receiver gates.

## CI provenance
B01-21 GitHub Actions will run real C# adapter code and original pinned MAPGEN planner compiled against openly-labelled synthetic UnityEngine/MapGenBuilder; no fabricated PhysX. It will package complete source+Unity metas+manifest into deterministic v1.1.0 ZIP and publish its SHA, lengths, code HEAD and per-case machine-readable receipts. Capture exact final successful Actions link and ZIP digest after runs (never reuse previous v1.0.1 archive/hash). B01-40 validates new GUID against target and B01-60 owns independent Unity acceptance.

## Codex-only remaining wiring, gated
When B01-00 G2 approves this specific revision, the receiver can store `snapshot.ReplayReceipt` in **existing authoritative save/progression** and call `adapter.TryReplay(receipt,...)` before spawning the same vehicle. On `InvalidReplayReceipt` or `ReplayGeometryMismatch` present a safe regenerate/new-run choice, **never** silently replace an active run or overwrite the saved seed. No host code ownership transfer. **NO MERGE / NO G2.**
