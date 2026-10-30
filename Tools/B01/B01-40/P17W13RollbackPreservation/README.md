# B01-40 — P17/W13 — ORIGINAL #510 rollback RED witness + versioned safety wrapper

## Scope: receiver/storage only (NO game source changes)

Sole frozen secure V2 candidate: B01-23 [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) at **`d94400d95d13db7f27e1533df05c9292d5380431`**, physical source ZIP **`d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387`**, 99,666 bytes, 35 members. Independent original-source-G0 [#521](https://github.com/C8LyNn/RoboRougelike/pull/521) is preserved unchanged as `ACCEPTED_SOURCE_ONLY`. This PR is a **NEW VERSIONED B01-40 wrapper**. It neither edits the frozen source nor reissues original `B01-23-SECURE-REATTACH-V2/2.0.0` as though it were repaired. #511 and #494 remain HOLD, no imported W07/W08/W09 scripts and no canonical Codex changes or merges.

## What actually fails in frozen producer

Original authored `Tools/B01/B01-23/P17W10SecureReattachV2/package.py::rollback` scans files and rejects symlinks, foreign/modified/missing files. However it **does not census directories** and ends with `shutil.rmtree(target)`, which deletes unrelated **empty directories** even if they contain no files. This is an actual data-loss hazard. This W13 harness imports and EXECUTES the **exact frozen original source** in an external disposable directory: original `build()` / `verify()`, original `stage()`, create a real foreign `FOREIGN-EMPTY-DIRECTORY/DEEPER-EMPTY-DIRECTORY` nested tree, invoke **original `rollback()`**, and assert the foreign empty directories were physically erased. This is the required source-level RED witness, not a source-only grep or simulated implementation.

## Original-byte guarded fix (our own new B01-40 scope)

The **NEW** W13 `receiver.py` never calls original unsafe rollback for any real receiver. It builds and validates the immutable original Git-source ZIP with original package.py but stages in a private NEW outside-Git temporary directory; only after all members are prepared does an atomic directory rename make the staged target visible. Own marker pins source+ZIP+35 original source members. Failure injection before the rename must leave no target and no partial staging directory.

`rollback_strict` first checks *every* file, original byte and marker, original directory set (including **unexpected empty children**), file type/foreign symlink or Windows junction, and changed/missing members; only a pristine owned staging target qualifies. On refusal the staged tree, foreign directory/file and their contents MUST remain. Successful deletion explicitly unlinks **only enumerated original files** and then removes **only enumerated original directories** bottom-up using `rmdir`; it never calls recursive `shutil.rmtree` on the receiver. Stage/rollback refuse a Git-managed path, repository subpaths and existing target, but **do not claim protection against malicious concurrent filesystem swaps**; use the approved offline disposable receiver and snapshot/backup if deploying into a broader live toolchain. A symlink probe is executed only when the OS permits symlink creation; if not, record `NOT_RUN_PERMISSION`, not PASS.

Negative probes: real original empty-directory destructive RED; new wrapper foreign empty direct/nested under Assets; changed/foreign/missing files; symlink/junction if supported; existing destination; injected mid-stage failure; forged marker; foreign W07 path; archive traversal attempt; untrusted changed expected manifest; two clean actual staging/rollback cycles. Immutable source byte/35-member manifest hashes are verified by *original author* build()/verify() and independent strict expected ZIP SHA.

## Save/Load and migration boundary (not hidden under G0)

[B01-62 #527](https://github.com/C8LyNn/RoboRougelike/pull/527) demonstrated that frozen original `AtomicDockLedger` and controller do **not** provide an approved persistent consumed/detached ID snapshot/restore API. This receiver wrapper intentionally does **not** create or guess a save schema and does **not** clear/rearm the ledger during load. **`SAVE_RELOAD_HOLD` is separate from the previously completed `SOURCE_G0_ACCEPTED_SOURCE_ONLY`.** For the owner B01-23 + B01-62's separate new versioned contract, mandatory refusal conditions include schema/version mismatch, wrong selected producer SHA, wrong run/vehicle/part stable ID, duplicate or missing IDs, detached-vs-consumed inconsistency, corrupt checksum/state, and load during detach/dock. Load should validate all modules and manifest atomically *before* materializing any Rigidbody, parent, transform or ledger change; failure leaves the prior state unmodified. Original #510 must remain frozen and unchanged; any required controller change gets a NEW version/SHA under owner approval.

The W13 wrapper does **not** inspect or migrate the user's current local Codex project or game save. B01-00 decides actual #79 source-only handoff and target SHA; B01-60 performs real Unity 6000.3.14f1 EditMode/PlayMode/PhysX with existing #532 runner when available, otherwise `NOT_RUN`; B01-61 independently validates transaction and saved IDs. A successful source/physical wrapper test is **not** Unity G1 or release G2. No fake ACK, no merge.

## Reproduce independently

GitHub Actions `b01-40-w13-frozen510-rollback-redgreen.yml` checks out only this B01-40 branch and exact frozen #510; on Ubuntu24.04 and Windows2025 with Python 3.12 it runs:

```bash
python receiver/Tools/B01/B01-40/P17W13RollbackPreservation/receiver.py \
 --producer producer510 --out dist
```

Artifacts: original executed destructive rollback witness JSON/stdout (never a fabricated native engine XML), positive/negative wrapper results and explicit `SAVE_RELOAD_HOLD / G1_NOT_RUN / G2_HOLD`. No project assets or live machine system files are changed; original producer's unsafe rollback is intentionally executed **only on a throwaway temp directory** and the new wrapper's own stage is also throwaway.

**Status until actual exact-final-HEAD two-OS logs:** `B01-40_ROLLBACK_FIXTURE_PENDING`; after both success, `ORIGINAL_FROZEN_ROLLBACK_RED_REPRODUCED / NEW_B01-40_WRAPPER_SOURCE_QA_PASS`. **Original source G0 for #510 remains accepted as a source archive, but unsafe author rollback must NOT be invoked on shared receiver directories.** `SAVE_LOAD_HOLD, NATIVE_G1_NOT_RUN, G2_HOLD, CODEX_ACK_NONE, NOT_INTEGRATED, NO_MERGE`.
