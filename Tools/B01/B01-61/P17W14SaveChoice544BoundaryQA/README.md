# B01-61 PASS17 WAVE14 — independent exact original #544 security boundary RED

**Package:** `B01-61-P17W14-SAVE-CONTRACT-544-ORIGINAL-BOUNDARY-QA-001/1.0.0`
**Target original B01-23 save contract:** [Draft #544](https://github.com/C8LyNn/RoboRougelike/pull/544) at exact SHA `e77529262b8f03c6e9f0e25b642038c351872044` / original Git blob `2dd3ee7f465a595ed42c2cbb6637200bb903185f`.
**ONLY selected gameplay source:** [frozen #510](https://github.com/C8LyNn/RoboRougelike/pull/510) @ `d94400d95d13db7f27e1533df05c9292d5380431`; original W06 `cb2b3a76e84266e2730fffc4ab048c1039f0652f`. Alternative gameplay #511/#494 HOLD, no co-install. This test does **not** alter the producer or select an owner contract for B01-23.

## Actual real source new W14 contribution (NOT duplicated #550)
[#550](https://github.com/C8LyNn/RoboRougelike/pull/550) independently detected two genuine mutable-after-validation bugs in different candidate **#549**. This project tests those same safety invariants **against a different B01-23 authored contract #544**, which was not covered by #550, to prevent falsely selecting #544 as a safe fix.

The .NET8/C#9 project compiles the original producer **#544** `StableIdSnapshotCodec.cs` directly, exact original **#510** `AtomicDockLedger.cs`, `SecureRigSpec.cs`, and original **W06** `DebrisImpulseCore.cs` pinned by Git object SHA. Only their canonical source files are executed; there is **NO copied controller, fake W06 ledger, test-only reimplementation of codec, or native Unity**.

Executable positive tests: four original attached/detached snapshot combinations, actual staged original W06 consumed IDs, original rollback compensation after injected exception, rejection on foreign run/stale generation, damaged checksum and load during active transaction.

Three independent negative security witnesses:
1. **`RunSnapshot.Parts`** is a mutable reference returned publicly AFTER validation. It can be replaced with the Heavy `PartRecord` while its already staged original W06 Light ledger still consumes Light ID.
2. **`PreparedRestore.StagedLedger(index)`** exposes the actual mutable original #510 `AtomicDockLedger`; an untrusted caller can invoke original `TryReattach` with a physical callback or `ClearForNewRun` directly, leaving the publicly visible snapshot `Detached+Consumed` but clearing its original W06 consumed ID.
3. **`TrustedManifest.Parts`** exposes mutable authority array after an accepted save; later callers can change the module-ID registry independently of staged original W06 ledgers.

These are **original-source API contract weaknesses** at the staging boundary, not a demonstration that a live Unity host is already exploitable; **there is NO authorized game Save/Load adapter in frozen #510**. A separate versioned owner schema must deep-freeze/copy all published arrays, conceal mutating ledgers and offer transaction-gated commit operations only. Never backfill legacy #549 or #544 by editing their frozen SHAs. Only after ONE new owner SHA is published can B01-61 re-run independent exact-new-SHA QA.

## Reproducible CI / strict release HOLD
GitHub Actions performs original Git SHA pin/scope checks, materializes the three frozen original #510/W06 dependencies using `git show` without checking out/changing those historical SHAs; `dotnet build --warnaserror` and direct `dotnet run` on Windows2025 & Ubuntu24.04. Evidence returns 0 **only when the actual original-source security RED is witnessed**, `--release` returns intentional 3 HOLD. Raw stdout and exit codes are archived.

```bash
python Tools/B01/B01-61/P17W14SaveChoice544BoundaryQA/source_pin.py
dotnet run -c Release --project Tools/B01/B01-61/P17W14SaveChoice544BoundaryQA/Original544BoundaryQA.csproj -- --evidence
```
The `Input/` files are created read-only by the GitHub workflow from exact original frozen Git objects and are not committed. This package has no receiver writes or rollback; close its unmerged Draft to undo additions. B01-40/62 own strict physical receiver migration and foreign empty directory safety from #536/#542.

**Gates:** `#510_GAME_SOURCE_G0_ACCEPTED_SOURCE_ONLY`; `#544_SAVE_CONTRACT_ORIGINAL_SOURCE_SECURITY_RED/HOLD` if reproduced, distinct `#549_HOLD` per independent #550, `SAVELOAD_HOST_NOT_INTEGRATED`, `UNITY_G1_NOT_RUN`, `G2_HOLD`, `CODEX_ACK_NONE`, `NO_MERGE`. Receivers B01-23/B01-62 (one versioned repaired schema), B01-40 (physical G0/migration), B01-60 (Unity #532), B01-00 (#79 queue).
