# B01-61 PASS17/WAVE14 — original #549 RED → selected original #556 source GREEN

Single selected B01-23 Save schema *decision* from [HQ #552](https://github.com/C8LyNn/RoboRougelike/issues/552): lineage #549 **frozen V1 source RED** `6da2c523e03766cd5a6deaed0cb3cccf8765354b`, one chosen new producer [Draft #556](https://github.com/C8LyNn/RoboRougelike/pull/556) **source R2** `33094f154c7b91119b36d58aab285e6e83c5de57`. Other producer #554 is a PARALLEL ALTERNATIVE whose different SHA/schema must not be coinstalled or implicitly accepted by this QA. Nonselected #544 stays HOLD. Frozen gameplay #510 remains exact `d94400d95d13db7f27e1533df05c9292d5380431`, #511/#494 gameplay ALT_HOLD.

## Independent test implementation, no copied original code
The *same* new B01-61 C#9 tester is separately compiled twice: once against **original byte-exact V1 #549** `DetachedSnapshotContract.cs` Git blob `6c036985411164372bf3d21d467780ff3777544e`, once against **original exact NEW V2 #556** Git blob `b52e511c370f5fca56c74029050844dc8cbb7f9a`, both with the actual unchanged W06 `DebrisImpulseCore.cs` and actual selected #510 `AtomicDockLedger.cs`. No Unity API or synthetic original business logic. Original bytes are separately `git show`-extracted into transient QA-only `Input/`; no gameplay, producer, V1, Codex or save source modified.

Executed matrix:
- Four two-rig Light/Heavy `Attached/Detached` permutations and actual original W06 consumed-ID cold staging.
- **#550 post-validation mutable snapshot witness**: original #549 `Current.Snapshot.Parts[0]` is mutable to another part; selected #556 defensive copy ensures validated backing record is unchanged.
- **#550 exposed original W06 ledger**: original #549 public `GetLedger` permits `RearmAfterReattach` without physical commit; selected #556 has NO public W06-returning method, only read-only state queries.
- **Additional independent busy/reentrant fault matrix**: calling original `ColdSessionHandle.TrySwap` recursively from host-idle callback: old source accepts nested mutation, new source returns `TransactionBusy`; host becoming busy after staging: old source checks once and publishes anyway, new version rechecks and refuses without replacing original cold pointer. Corrupt/checksum/run/generation/busy/throw rejection preserves previous pointer.
- **Critical remaining blocker**: new original frozen #510 `AtomicDockLedger` starts empty even when standalone #556 cold W06 plan reconstructed consumed IDs. The selected #510 V2 controller has no authorized atomic hydrate/swap API, so these source checks **CANNOT attest actual game Save/Load**.

Runbook:
```bash
python Tools/B01/B01-61/P17W14ExactSave556RegressionQA/source_guard.py
dotnet run --project Tools/B01/B01-61/P17W14ExactSave556RegressionQA/OldOriginalSourceQA.csproj -c Release -- --red
dotnet run --project Tools/B01/B01-61/P17W14ExactSave556RegressionQA/NewOriginalSourceQA.csproj -c Release -- --green
```
Caution: `Input/` is untracked Git-original test material created by the Actions workflow first, not committed into this QA PR. Old returns deliberate exit **3**, new exits **0**, and CI checks exact codes on Windows2025+Ubuntu24.04. Logs/builds/receipts uploaded as workflow artifacts.

**No integration:** #510 SOURCE_G0_ACCEPTED_SOURCE_ONLY historical #521; new #556 logical contract SRC QA only; new B01-40 physical G0 and B01-62 ID validation are separately required. Native Unity 6000.3.14f1 G1 **NOT_RUN** and actual game SaveLoad **HOLD**, G2 HOLD, CODEX ACK NONE/NOT_INTEGRATED, no merge. *Strict migration/rollback* belongs #536/#542, raw old #510 rollback deletes foreign empty directories; never call it on a shared receiver. Other #554 fork cannot reuse this exact SHA receipt. Rollback close QA-only Draft, remove only six added paths, original producer unchanged.
