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

## W14 exact repaired candidate cross-check (independent GREEN)

After confirming the historic #549 defect on original unchanged source, the same dual-OS QA workflow also directly compiles **new corrected producer [#556](https://github.com/C8LyNn/RoboRougelike/pull/556)** exact immutable candidate commit `33094f154c7b91119b36d58aab285e6e83c5de57`, **real source Git blob** `b52e511c370f5fca56c74029050844dc8cbb7f9a`. CI obtains the **actual original Git bytes** with `git show <exact-SHA>:Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs` into a temporary `dist/candidate/` path; it rejects any incorrect Git blob before running the second C#9/.NET8 project. Neither producer's source is edited.

Eight NEW independent corrected-candidate source assertions test: actual W06 consumed-ID restoration, rejecting a nested swap when idle returns false, rejecting nested swap when idle throws, nested generation-block during successful outer swap, second idle callback refusal without publication, post-publish snapshot copy, lack of public original W06 mutable ledger exposure, and corrupted save refusal preserving the prior session.

Only if actual native runner logs show **both** `TOTAL 6 PASS 6 FAIL 0` (#549 expected RED reproduction) and `TOTAL 8 PASS 8 FAIL 0` (#556 expected GREEN correction) will CI emit the evidence JSON, including both source SHA/blob pins, exact QA HEAD and full owned-path SHA256 list. Native Unity, real scene swap, G1 and G2 are explicitly not claimed. Other #554 alternative remains separate pending HQ single-source selection; do not install competing producer revisions together.

Manually reproduce corrected check at the repository root with the exact SHA already fetched:

```sh
mkdir -p dist/candidate
git show 33094f154c7b91119b36d58aab285e6e83c5de57:Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs > dist/candidate/DetachedSnapshotContract.cs
dotnet build Tools/B01/B01-61/P17W14ColdIdleReentryQA/CorrectedGuardTests.csproj -c Release -p:CorrectedSourcePath=../../../../dist/candidate/DetachedSnapshotContract.cs
dotnet Tools/B01/B01-61/P17W14ColdIdleReentryQA/bin/Release/net8.0/CorrectedGuardTests.dll | tee qa_green.log
```
