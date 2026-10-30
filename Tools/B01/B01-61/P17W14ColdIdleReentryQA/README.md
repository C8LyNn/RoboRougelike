# B01-61 / PASS17 WAVE14 — independent real C# cold host idle-reentrancy QA

**Purpose:** reproduce a distinct transaction-safety defect in the ONE candidate portable detached-ID Save/Load contract [#549](https://github.com/C8LyNn/RoboRougelike/pull/549), without modifying its source, the selected frozen gameplay [#510](https://github.com/C8LyNn/RoboRougelike/pull/510), or the producer-owned branches.

## Exact inputs / immutable scope

- Selected gameplay #510: `d94400d95d13db7f27e1533df05c9292d5380431` (FROZEN).
- W13 Save contract #549 original exact HEAD: `6da2c523e03766cd5a6deaed0cb3cccf8765354b`.
- Actual production `DetachedSnapshotContract.cs` Git blob: `6c036985411164372bf3d21d467780ff3777544e`.
- Actual original W06 `DebrisImpulseCore.cs` Git blob: `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`.
- Only additive files in this own QA directory and one QA-specific workflow. No Unity source, no host adapter, no #510 producer code edited.

## Defect and impact

`ColdSessionHandle.TrySwap` first executes supplied `hostIdle()`, and sets `swapping=true` only **after** decoding and checking generation. A reentrant `hostIdle` callback can call `TrySwap` again while the guard is still false. The nested swap publishes a new logical session. The outer call can subsequently return `TransactionBusy`, `Quarantine`, or `StaleGeneration`, **despite the session having been changed during its execution**. This violates failure-atomicity at the cold logical session boundary.

This is independent of the two producer defects in [#550](https://github.com/C8LyNn/RoboRougelike/pull/550): published mutable `RunSnapshot.Parts` and externally exposed mutable W06 ledger. All three defects need separately versioned producer correction and independent retest. The remedy should hold a non-reentrant guard across idle callback + validation + publication, and fail closed without publishing on rejected outer operations. Do not edit the frozen source to make the QA turn green.

## Reproduce

```sh
dotnet run --project Tools/B01/B01-61/P17W14ColdIdleReentryQA/ColdIdleReentryQA.csproj --configuration Release
python Tools/B01/B01-61/P17W14ColdIdleReentryQA/receipt.py --log qa_test.log --out dist/b01-61-p17w14-reentry-qa.json
```

The second command requires a real captured `qa_test.log` from the first. Three baseline assertions prove original logical Save/Load works under nonreentrant conditions. Three **RED witness** assertions directly compile and invoke the exact production source together with the unchanged original W06 ledger (no mocks, fake controllers, reflection, or Unity shims). The expected source QA result is `TOTAL 6 PASS 6 FAIL 0` **only if the original unsafe behavior is actually reproduced**. It means the **producer release gate is RED**, not that Save/Load is safe.

## Gate / receiver

`B01-61_QA_WITNESS_PASS` does not confer `B01-23_CONTRACT_ACCEPTED`. B01-23/62 owns versioned source correction; B01-61/62 independent QA must retest exact repaired SHA; B01-40 owns safe packaging/migration; B01-60 runs native Unity/PhysX using existing #532 with a licensed Unity 6000.3.14f1 host or records `G1_NOT_RUN`. The original #510 rollback must not be executed in a shared receiver because it can delete foreign empty directories (see #536). `G2_HOLD`, no merge, no Codex ACK or live integration.

**Rollback:** abandon this isolated QA branch; no runtime or existing source modified.
