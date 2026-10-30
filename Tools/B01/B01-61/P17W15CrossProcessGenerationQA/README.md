# B01-61 / PASS17 WAVE15 — cross-process generation authority QA

**Assignment:** [#571](https://github.com/C8LyNn/RoboRougelike/issues/571) B01-61 independent Save/Load + transaction QA. **Only selected immutable Save contract:** [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) commit `33094f154c7b91119b36d58aab285e6e83c5de57`, actual C# source blob `b52e511c370f5fca56c74029050844dc8cbb7f9a`, original unchanged W06 blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`. Gameplay [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) `d94400d95d13db7f27e1533df05c9292d5380431` remains frozen. #544/#549/#554 are HOLD. No mutation to gameplay, Save schema or Codex code.

## What these tests DO test

Unlike previous #550/#563/#570 single-process or original-source security tests, this standalone executable starts **separate real .NET 8 child processes** using `Process.Start`, writes actual binary snapshots through the **original #556 SnapshotCodec** into fresh OS temporary files, then launches fresh independent processes to `ColdSessionHandle.TrySwap` and read the genuine original W06 cold consumed-ID flags. Tests verify Light/Heavy stable ID persistence, actual bytes on disk, wrong run, corrupted/truncated data, and valid-SHA re-signed foreign ID refusal. A single live host also refuses a rollback from generation 9 to 8 without mutating its previous current plan.

## New release-critical host bridge requirement: durable anti-replay anchor

The source-only `ColdSessionHandle` has no **persisted highest committed generation** or commit transaction ID. Fresh host/process with `minGeneration=1` accepts a valid older save (generation 8) even after another fresh process has successfully loaded generation 9. Same-run same-generation **divergent** physical saves are also both accepted by fresh cold sessions. This is **expected from #556's documented logical API**, not evidence that #556 violates its own portable contract. It is a concrete security acceptance condition for the **yet-to-be-delivered Unity host adapter**: the host must maintain independently durable monotonic run/generation/commit authority (not stored only inside the replaceable save file), reject stale/forked commits and retain existing scene/body/ledger state on every refusal. Durable authority crash recovery itself needs a clear transaction protocol (save file + journal/watermark; fsync/atomic replace and recovery rules) and live independently tested host implementation. Passing these tests **does not** claim that required host bridge exists.

**Ten independent assertions** (source QA PASS means limitation reproduced and host release still HOLD): three fresh-process writes/normal load, 2 cross-process anti-replay/fork proofs, 5 failed/valid reference controls; exact counts are printed by the runtime. Results are from executed originals, NOT Unity/PhysX or a mocked bridge. A separate new adapter should be tested by B01-61 at **its own new exact commit SHA** and accepted only with actual native Unity G1 where appropriate.

## Commands

```sh
dotnet run --project Tools/B01/B01-61/P17W15CrossProcessGenerationQA/CrossProcessGenerationQA.csproj -c Release | tee qa_test.log
python Tools/B01/B01-61/P17W15CrossProcessGenerationQA/receipt.py --log qa_test.log --out dist/b01-61-w15-generation-qa.json
```

GitHub Actions runs actual Windows2025+Ubuntu24.04 .NET8 and records exact original Git blobs, 5 new QA-only file hashes and full output. Only scratch temporary OS save files are created; no canonical scene, no game repository writes except this isolated QA branch. No Unity Editor XML/PhysX is claimed. `G0_SELECTED_556_SOURCE_ONLY / HOST_ADAPTER_QA=NOT_RUN_NO_CANDIDATE / NATIVE_G1_NOT_RUN / SAVELOAD_ACCEPTED_HOLD / G2_HOLD / CODEX_ACK_NONE / NO_MERGE`.

**Next receiver:** B01-23/62 bridge author must provide real adapter exact SHA with an owned public API for real Unity/ledger transactional staging and durable commit authority. B01-61 independent tests that exact source, B01-40 validates safe staging and rollback, B01-60 executes native Unity. New QA branch is disposable and can be abandoned without touching gameplay.
