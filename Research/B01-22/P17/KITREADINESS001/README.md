# B01-22 / KITREADINESS-001 R1 — Virtual Kit Stock Readiness

**Scope**: independently owned read-only Unity-ready C#9 tool, no garage, wallet,
shop, save, damage, run, rewards or authoritative stock writes. Frozen catalog
source [#411](https://github.com/C8LyNn/RoboRougelike/pull/411)
at `b1d4e341d846c9397e236809a1d7e4c4fff9c935` (Codex gameplay #383
`203a0805251ed28293888e43f2d49f0df63767c6`). Stacked PR against #411
for source reuse only; parent must not be unintentionally merged into game.

## Real functionality
`KitReadinessPlanner.Preview(sourceSha, exact10Catalog, stockCounts, orderedRequests)`
returns up to 100 immutable-preview decisions for real EK01–EK10 physical kits.
Every result reports which physical kinds/weapon families are missing, required
vs available counts, and **reference** price for missing pieces. Successful
requests reserve parts in a private in-memory ledger. Failed requests reserve
**nothing** (atomic), so later eligible requests can still succeed. Duplicate
ordered requests are allowed; the first uses stock, later may be blocked.
Changing request order can change the attainable assortment, intentionally.
The function does not mutate caller stock and has no UnityEngine dependency.
It does not decide spending and should NOT be bound to a live wallet or save.

## Data/reproduce
Three real authored scenarios in `scenarios/`: starter-cache (first EK01 ready
and second blocked), siege-cache (EK08 ready, EK02 blocked), shared-gyro
(EK01 ready, EK08 blocked, EK02 ready). The original C# source test executable
loads real frozen #411 catalog, computes real reference unit prices and emits
three deterministic JSON `*-preview.json` receipts. No fake baked outcome.
All source C# compiled under .NET8 in GitHub Actions (C#9 compatibility).
These source tests are NOT native Unity Editor/PlayMode/Player tests.

```sh
dotnet run --project Research/B01-22/P17/KITREADINESS001/KitReadinessTests.csproj --configuration Release -- --out readiness-previews
python -m unittest discover -s Research/B01-22/P17/KITREADINESS001 -p test_pack_readiness.py -v
python Research/B01-22/P17/KITREADINESS001/pack_readiness.py --stage readiness-artifact --previews readiness-previews
python Research/B01-22/P17/KITREADINESS001/pack_readiness.py --rollback readiness-artifact
```

Physical GitHub Actions artifact includes original importable C# and two stable
Unity GUID .meta files in `kitreadiness-unity.zip`, plus the THREE real scenario
outputs and SHA-256 receipt, reproduced on Windows2025 and Ubuntu24.04.

## Import/rollback contract
B01-40 must independently verify 2 unique GUIDs and no collision with actual
target before importing only `Assets/B01KitReadiness001.meta` and the
`Assets/B01KitReadiness001/**` files in a disposable licensed Unity project.
NO automatic import, no gameplay binding and no Codex code changes.
To roll back source copy, delete only these isolated owned files after verifying
destination absent before install. For source zip stage, `--rollback` refuses
foreign or modified files; no blind deletes. To remove this Git-only research,
close its Draft PR and discard its own new branch; never merge to main/develop.
Native Unity G1 and independent B01-40/B01-60 QA remain NOT_RUN/PENDING
until actual execution. B01-00 alone decides #79 OFFERED; Codex ACK not assumed.

## PASS17 W08 cross-platform parity fix
Original raw Windows/Ubuntu first-run inner source ZIP SHA matched, but preview JSON hashes differed due platform-dependent C# JsonSerializer indentation (CRLF/LF). The original C# scenario writer now normalizes CRLF to LF before writing each generated JSON; physical receiver rejects CR-containing or non-newline-terminated preview files and has a dedicated CRLF negative test. Compare **raw inner** source ZIP and all three original preview JSON hashes across Windows and Ubuntu; outer GitHub Actions ZIP hash is not the evidence.

## PASS17 WAVE09 — v1.0.1 / original source BOM integrity RED → GREEN

**Actual independent source-compiler RED was reproduced** against v1.0.0 original C# before any fix, on Windows2025 AND Ubuntu24.04: [GitHub Actions #41052841055](https://github.com/C8LyNn/RoboRougelike/actions/runs/41052841055), `RED REJECT coerced unit scrap with old source SHA` (test 50). The original planner required an exact catalog SHA **string**, ten ordered IDs, valid quantities, no duplicates, and an upper bound on unit scrap, but did not verify that these caller-provided parts/prices MATCH immutable #411 BOMs. Fake but structurally valid requests could therefore yield altered shortfall prices and buildability.

**Fix owned by B01-22 only:** before any virtual stock/reservation is processed, `KitReadinessPlanner.Preview` now calculates SHA256 over 154 ordered, length-prefixed UTF-8 encoded semantic values from the actual source catalog: approved source commit SHA, kit count, 10 exact IDs, ordered BOM row counts, each kind, weapon subtype, part quantity and approved scrap unit quote. All integers invariant culture; token length is UTF-16 C# string code units. Expected source digest `6f121fe4207aaca2f30dfede9e79700d3af0dbec5349c87c1709c174dd4096eb` was independently derived from the immutable original #411 `catalog.json` Git blob `372b3bc74553ea459e89be38378f4429090e00a2`, *not* from a fabricated data fixture. This source snapshot does not authorize purchases or protect from malicious replacement of the C# code itself. Changes to canonical kit BOM/quotes require new source version and a new owner-approved digest.

**New original C# negative cases:** coherently forged unit price, valid-but-changed part count, substitution of another valid physical part kind with a plausible price, and reordering two real source BOM rows — all required to reject while catalog SHA field stays unchanged. Original test count 53. **New independent Python cases:** recompute the exact approved SHA256 from real original catalog with a distinct implementation, demonstrate modified price and row order produce different digests; original Python test count 19. Both original code and real physical ZIP/receipt must pass Windows+Ubuntu on the exact new HEAD.

**Handoff:** supersede W08 producer SHA `aca5da31b756edee795af1c5e001dd134902c258` only after exact-head W09 run finishes successfully; do not erase the previous immutable evidence. #461 remains Draft, not OFFERED/FROZEN by B01-40, no Codex ACK or game integration. Continue independent B01-40 G0 and B01-60 exact new-head QA; native Unity 6 Editor/Play/Player still NOT_RUN unless separately evidenced.

## PASS17 WAVE10 — v1.1.0: real user-provided equipment stock input

NEW **working read-only C#9 CLI** uses **the same original `KitReadinessPlanner` source** as the Unity component, not a separate mock planner. It accepts an exact JSON contract (example `scenarios/user-request-example.json`) with `schema=b01-kitreadiness-request-v1`, approved immutable `source_sha`, `reference_only=true`, `requests` (0–100 real EK01–EK10 IDs in explicit priority order) and `owned_parts` (physical kinds/counts, optional `weapon` subtype for Gun). The input can be authored by a person or external tool; no game integration, inventory adapter or wallet access.

```sh
dotnet build Research/B01-22/P17/KITREADINESS001/ReadinessCli.csproj -c Release
dotnet Research/B01-22/P17/KITREADINESS001/bin/Release/net8.0/ReadinessCli.dll --input Research/B01-22/P17/KITREADINESS001/scenarios/user-request-example.json --out my-readiness-preview.json
```

The command evaluates real source-accepted #411 kits, validates every input field, rejects duplicate/foreign JSON properties and invalid stock and IDs, and **does not create output on validation failure**. It writes an exclusive NEW UTF-8/LF JSON output (never overwrite) with each request's actual ready/blocked, missing kind/weapon/quantity and reference Scrap shortfall, and a private unpersisted remaining-stock snapshot. It refuses outputs within `Assets/`, `Packages/`, `ProjectSettings/`, `Library/`, `UserSettings/` and `.git/` for the selected repository root. Reference preview does **not** buy parts, reserve real stock, modify live game state, or authorize any mounting.

Original cross-platform CI compiles and executes this production C#9 CLI, runs 16 black-box positive/negative **real child process** tests (including double JSON keys, forged source, no-overwrite, false reference flag, invalid stock, duplicate items, malformed inputs and 101 requests), and generates a fourth actual `user-preview.json` from `scenarios/user-request-example.json`. The original physical packager now verifies **all four** C#-produced preview JSON files (three historical authored scenarios + new user input) against a SHA256 receipt, together with the 3-member Unity source ZIP; cross-OS byte parity is mandatory. The CLI files are NOT copied into Unity; import scope remains only `Assets/B01KitReadiness001/**` plus root `.meta`.

This is **revision 1.1.0 of existing open producer Draft #461**, not an additional package or independent QA acceptance. Supersedes previous candidate 1.0.1 for a future receiver. Native Unity Editor/PlayMode is still `NOT_RUN`; B01-40 G0/B01-60 independent QA pending.
