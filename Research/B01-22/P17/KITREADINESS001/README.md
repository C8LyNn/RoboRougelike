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

W10 source-safety hardening: output path verification also rejects any symlink/reparse-point *parent directory*, so lexical location guards cannot be bypassed via a link into Unity project paths. New original CLI process negative test asserts the isolated `Assets/B01KitReadiness001/` folder remains unmodified even if directly selected as output. Exact candidate must rerun both CI OS.

## PASS17 WAVE11 — v1.2.0: consolidated batch equipment shortfall (actual C#9)

The new **`procurement`** field in the real `ReadinessCli` JSON output
summarizes *all* requested EK01–EK10 recipes at once. It aggregates every
physical part kind + Gun weapon variant across repeated and overlapping kits,
covers required quantities from the user's stock **exactly once**, and returns
per-line `Required`, `Owned`, `Covered`, `Missing`, original reference
unit Scrap, missing Scrap quote, plus total required/covered/missing physical
pieces and total reference Scrap for the entire requested batch.

This **differs materially** from earlier per-request `READY/BLOCKED`
simulation: a failed/blocked request does not reserve virtual parts, but
the procurement forecast still counts the materials needed to eventually
assemble **every requested kit** without double-charging overlapping stock.
Thus two EK01 requests with one EK01's parts owned require exactly another
5 pieces, totaling **110 reference Scrap**, not repeated or compounded
single-request shortfalls. Reordering requests may change atomic build
priority but never changes batch procurement totals.

Implementation is original C#9 `Research/.../KitProcurementForecast.cs`,
compiled and executed through the existing real user-input CLI; it invokes
the *existing* source-validated `KitReadinessPlanner.Preview` before
forecasting. **Research-only: no new Unity asset, no gameplay or wallet write,
no checkout/cart, no purchasable order or price authority.**
Six extra black-box process tests execute real C# and check empty/full stock,
duplicated kits, line aggregation, totals, and order invariance. Keep original
three-file Unity source ZIP unchanged; four generated JSON previews and
receipt reflect the updated CLI user-preview output. Native Unity
Editor/PlayMode = NOT_RUN, pending independent B01-40/B01-60.

## PASS17 WAVE12 — v1.3.0: exact bounded build-order advisor

A genuine new **`buildOrder`** result in the executable read-only C#9 CLI uses
the **original source-pinned `KitReadinessPlanner.Preview` for every unique
permutation** of up to **six** requested kits, and returns a request sequence
that yields the maximum count of `READY` builds without adding parts. It
preserves the original sequence when scores tie (minimum moved positions)
and de-duplicates identical kit labels in the enumeration. The planner is
unmodified and there is no new Unity asset, Rigidbody, wallet, game state or
in-game integration.

The actual fixture `scenarios/order-priority.json` starts with
`EK08, EK01, EK02` and a contested gyro/track/Heavy stock pool.
The submitted sequence makes **1** kit buildable; the suggested sequence makes
**2** kit builds possible using *exactly the same original C# planner*.
The total procurement shortfall is independent of this reorder.

For lists **longer than six**, the field explicitly reports
`Status=NOT_EVALUATED_REQUEST_LIMIT`, `OrdersEvaluated=0` and returns
the unchanged original request order; it **does not pretend** an exhaustive
optimum was found. This cap bounds factorial cost and avoids freezing a
game runtime. The research-only CLI handles up to 100 requests as before.

Reproduce from repository root after the same Release build:

```sh
dotnet Research/B01-22/P17/KITREADINESS001/bin/Release/net8.0/ReadinessCli.dll --input Research/B01-22/P17/KITREADINESS001/scenarios/order-priority.json --out new-order-preview.json
```

Eight additional real CLI subprocess tests verify actual 1→2 improvement,
replay suggested order, duplicate requests, >6 explicit no-optimum status,
zero requests, fully buildable original-order ties, repeatability/nonmutation
and six repeated kit requests. The physical package includes the real
`priority-preview.json` generated from this input and its independent SHA256
receipt. Source-only original Unity ZIP remains 3 members unchanged. This is
a **decision-support preview, not automatic garage scheduling or purchases.**
Independent B01-40 G0, B01-60 QA and native Unity G1 remain pending.

## PASS17 WAVE13 — v1.3.1 / actual original materialization integrity RED→GREEN

Reproduced vulnerability: previous producer `validate_sources()` checked the Git `HEAD:path` SHA1 reference and existence of each path, but did **not** verify **on-disk bytes**; a locally modified original C# or original #411 catalog could enter a new ZIP despite an unchanged approved Git manifest. Original RED tests safely mock byte reads (no actual Unity/game source edited) for a modified physical C# file and frozen original catalog. New `git_blob_oid` computes real Git blob SHA1 (`blob <size>\\0<bytes>`), compares every of 19 physical source files and frozen #411 catalog against HEAD + immutable manifest; the actual three Unity asset byte snapshots are checked a second time before archive creation, failing closed even if bytes changed after initial validation. A source mutator can never be trusted to audit itself; these checks detect accidental or external dirty checkout materialization, not malicious rewritten producer source or cryptographic authenticity.

Additional original real negative tests verify: **nested foreign EMPTY directory** inside the package output prevents rollback without deleting any foreign or owned file; **foreign directory symlink** is preserved (skips only if OS refuses link creation); **asset mutation after precheck** does not create destination; Git empty-blob standard SHA1 and original planner blob match. `stage()` only creates a new directory once all source and preview checks pass, and `rollback()` never blindly recursively removes paths. The physical three-member Unity source ZIP is expected to retain previous bytes and hash; five actual C# JSONs should be unchanged. Dual-OS exact final SHA C# source, Python physical tests, original Git pins, ZIP+5 JSON receipts required before declaring producer PASS. Native Unity, independent B01-40/B01-60 G0/G1 still pending. Frozen Secure Reattach #510 and W13 Save/Load work are strictly out of scope.

## PASS17 WAVE14 — v1.3.2: late receiver-state mutation before rollback

Two new ORIGINAL executable rollback negative tests reproduce a concrete source-security failure of v1.3.1 on a fully isolated temporary producer stage. At the boundary **after receipt verification** but before deletion, another actor creates a foreign **empty subdirectory**: the old code unlinked owned ZIP, five previews and receipt, and only then failed at `rmdir`, leaving foreign state with missing owned artifacts. Alternatively, an owned `receipt.json` changed after validation was deleted despite no longer matching the verified bytes. Both tests patch only the verification callback and **never touch game assets or Codex**.

The new `owned_rollback_snapshot` requires exactly seven expected direct-child files, no foreign files or **empty directories**, no symlinks, and captures actual bytes. Rollback now compares the complete directory layout and every original owned file's bytes **after** all parsing and receipt validation callbacks, **before** unlinking any owned file. Any new empty directory or mutation produces a fail-closed `SourceError` and preserves the archive and all previews intact. This is a deterministic *post-validation pre-deletion* integrity gate, NOT a claim of OS-atomic deletion against hostile concurrent writers. On a live shared directory, only one exclusive writer/owner and an authorized receiver transaction can establish full atomicity; never run this rollback against the frozen #510 gameplay receiver. Original physical source package, 3 Unity members and five generated JSON previews remain unchanged; native Unity/G1 remains NOT_RUN.

## PASS17 WAVE15 — v1.3.3: independent preflight without rollback

The new **actual CLI command** `python Research/B01-22/P17/KITREADINESS001/pack_readiness.py --verify-stage <staged-dir>` performs **read-only** acceptance of one B01-22 stage in a clean original Git checkout. It first validates all 19 actual worktree Git blobs and original immutable #411 catalog, refuses symlinked paths, then compares the complete set of exactly seven own direct child files (including **foreign EMPTY directories**), parses all five actual C# preview JSONs, checks the 3-member Unity-source ZIP against the physical immutable source, validates the full SHA256 receipt, and checks all seven file bytes again after the verification callback. It prints `VERIFIED_STAGE_READ_ONLY` and receipt on success; on any mismatch prints `FAIL_CLOSED`, exits 2 and **does not mutate the stage**. Does not call rollback, delete or copy files; this is the safe G0 inspection entrypoint for B01-40. Actual files may still change after the audit; a passing report does not confer exclusive ownership, authorize mounting or imply live Unity integration. Avoid all existing alternate #510/#556 stage/rollback paths.

Five original positive/negative Python tests 28–32 and the actual CI source-step execute this command and confirm: repeatable read-only intake, rejection of a foreign EMPTY directory, rejection of an altered receipt without data loss, successful real child process with unchanged seven bytes, and failed real child process that preserves its foreign and owned bytes. Exact final HEAD requires original C#9/CLI and physical ZIP receipts on Windows and Ubuntu; B01-60 native Unity remains NOT_RUN until separately executed.

## PASS17 WAVE16 — v1.3.4: independent pinned output receipts

Actual original RED CI showed two coherent forgeries: source-compatible C# preview result values were edited and the receipt SHA256 was recalculated, while old read-only G0 inspection approved both. The new acceptance checks all six physical payload hashes and sizes against the original W15 Windows/Ubuntu identical production outputs, pinned in versioned Git manifest. It also compares the real worktree manifest against HEAD:manifest.json Git SHA1 before trusting those authorization values. Production stage rejects modified preview bytes before creating the receiver path; existing test-only stage(source_check=False) remains limited to synthetic rollback fixtures. New negative tests 35/36 cover dirty manifest and forged production preview. Future legitimately changed C# output requires an explicit new producer version and independent QA, never automatic repinning. Native Unity/G1 NOT_RUN and unrelated frozen Secure #510/Save #556 untouched. This is Git provenance within trusted repository, not a digital signature against repository compromise.
