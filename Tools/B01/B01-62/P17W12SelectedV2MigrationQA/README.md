# B01-62 — PASS17 / WAVE12 — SELECTED-V2-SAVE-MIGRATION-QA/1.0.0

## Scope and exact ownership

Independent read-only intake of **only** B01-23 Secure V2 R2 [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) at immutable source SHA `d94400d95d13db7f27e1533df05c9292d5380431`, as selected in [#518](https://github.com/C8LyNn/RoboRougelike/issues/518). Alternatives #511 and #494 are **excluded**, never staged or tested as the selected revision. Previous B01-62 #486/#504 evidence remains immutable; B01-61 [#517](https://github.com/C8LyNn/RoboRougelike/pull/517) already executes the original C# ledger and host transaction. **This package deliberately does not duplicate that harness.** It covers the distinct unresolved **save-state / migration acceptance boundary**.

Five actual original Git blobs are read via pinned `git show <commit>:<path>`, checked by independently recomputing Git SHA-1 over raw blob bytes and checking `git rev-parse` object IDs. `manifest.json` contains immutable producer SHA, source object hashes, exclusive self-owned paths and G0/G1/G2 boundaries. There are no producer, Codex, Unity Assets, game scenes, or actual save files edited.

## New substantive, executable QA

`intake.py` verifies original #510 has a private ledger, `TryReattach` reserve/rollback/compensation hooks, all three public routes through the same dock gate, physical broadphase/penetration source calls and a new empty scene builder with the protected controller only. It refuses drift in the selected original methods, legacy W07/W08/W09 AddComponent calls, changed original rig stable IDs, altered source blobs, alternative candidate SHA and unknown fixture format.

**Save limitation:** the selected R2 standalone demo does not itself define a persistent consumed-ID serializer/deserializer and its original source has no canonical Save/Load contract. Therefore no reliable rehydration of a detached part's consumed stable ID is established for the actual game. The QA preflight deliberately **HOLDs all fixture snapshots with detached or consumed IDs**, invalid revisions, duplicated/missing IDs and mixed legacy/alternative components. A clean all-attached fixture can be staged to an entirely new **external disposable scratch directory only**, with canonical byte hashing and a receipt. Rollback deletes the staging folder only after exact membership and byte re-verification; it refuses foreign/mutated/missing members or symlinks and will not touch an existing target. **This is NOT the game's save schema, actual save migration, or integration-ready loader.**

`test_intake.py`: 34 independently authored positive/adversarial controls for schema, stable IDs, two rigs, binary component exclusivity, denied detached/consumed snapshots, tamper/dirty rollback, foreign-file preservation, and source-route contract mutations. GitHub Actions runs on Ubuntu24.04 + Windows2025 and publishes exact original-source JSON receipts, with a third byte-parity job.

## How to reproduce

In a disposable checkout with Python 3.12 and Git:

    git fetch --no-tags origin d94400d95d13db7f27e1533df05c9292d5380431
    python -m unittest discover -s Tools/B01/B01-62/P17W12SelectedV2MigrationQA -p test_intake.py -v
    mkdir -p qa-evidence
    python Tools/B01/B01-62/P17W12SelectedV2MigrationQA/intake.py --repo . --output qa-evidence/receipt.json

Receipt output path must be new. Isolated staging and rollback are exercised by tests using temporary directories outside the repo. There is no command to apply the fixture to canonical game files.

## Formal gates and receiver

- **B01-62 source QA**: only for exactly `#510@d94400d9...`; fixture migration acceptance of clean all-attached data is **QA_ONLY**. Native Unity save/load persistence has **NOT_RUN**; a successful QA run is not evidence of game save compatibility.
- **B01-40**: own formal G0 physical original 35-member ZIP, 14 GUID, 3 asmdefs, selected source-only acceptance, actual authorized receiver SHA, exclusive migration and strict rollback. B01-62 does **not** sign their physical G0.
- **B01-60/61**: original selected R2 tests for lost-ID / all 3 aliases / both physical rigs / injected rollback, and *real* licensed Unity 6000.3.14f1 native PlayMode/PhysX with actual save/load or explicit NOT_RUN.
- **B01-23**: if persistence of consumed IDs is in scope, separately version and expose a **validated and atomic** save/restore API for stable IDs before any host migration. Never modify frozen #510.
- **B01-00**: sole Codex #79 coordinator. #510 candidate reservation does not imply CODEX_ACK, an actual GAME installation, or G2.

**Status boundary:** `SOURCE_QA_ONLY / SAVE_LOAD_HOLD / NATIVE_UNITY_NOT_RUN / G2_HOLD / CODEX_ACK_NONE / NO_MERGE`. Rollback: discard only this independent B01-62 branch / files or remove a verified disposable fixture directory. Historical #486 RED stays historically correct; #517 exact-source transactional GREEN is not a persistence PASS.
