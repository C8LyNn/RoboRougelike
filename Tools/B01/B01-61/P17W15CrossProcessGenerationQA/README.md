# B01-61 / PASS17 WAVE15 — cross-process generation authority QA

**Assignment:** [#571](https://github.com/C8LyNn/RoboRougelike/issues/571) B01-61 independent Save/Load + transaction QA. **Only selected immutable Save contract:** [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) commit `33094f154c7b91119b36d58aab285e6e83c5de57`, actual C# source blob `b52e511c370f5fca56c74029050844dc8cbb7f9a`, original unchanged W06 blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`. Gameplay [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) `d94400d95d13db7f27e1533df05c9292d5380431` remains frozen. #544/#549/#554 are HOLD. No mutation to gameplay, Save schema or Codex code.

## What these tests DO test

Unlike previous #550/#563/#570 single-process or original-source security tests, this standalone executable starts **separate real .NET 8 child processes** using `Process.Start`, writes actual binary snapshots through the **original #556 SnapshotCodec** into fresh OS temporary files, then launches fresh independent processes to `ColdSessionHandle.TrySwap` and read the genuine original W06 cold consumed-ID flags. Tests verify Light/Heavy stable ID persistence, actual bytes on disk, wrong run, corrupted/truncated data, and valid-SHA re-signed foreign ID refusal. A single live host also refuses a rollback from generation 9 to 8 without mutating its previous current plan.

## New release-critical host bridge requirement: durable anti-replay anchor

The source-only `ColdSessionHandle` has no **persisted highest committed generation** or commit transaction ID. Fresh host/process with `minGeneration=1` accepts a valid older save (generation 8) even after another fresh process has successfully loaded generation 9. Same-run same-generation **divergent** physical saves are also both accepted by fresh cold sessions. This is **expected from #556's documented logical API**, not evidence that #556 violates its own portable contract. It is a concrete security acceptance condition for the **yet-to-be-delivered Unity host adapter**: the host must maintain independently durable monotonic run/generation/commit authority (not stored only inside the replaceable save file), reject stale/forked commits and retain existing scene/body/ledger state on every refusal. Durable authority crash recovery itself needs a clear transaction protocol (save file + journal/watermark; fsync/atomic replace and recovery rules) and live independently tested host implementation. Passing these tests **does not** claim that required host bridge exists.

**Ten independent cross-process assertions** (source QA PASS means limitation reproduced and host release still HOLD): three fresh-process writes/normal load, 2 cross-process anti-replay/fork proofs, 5 failed/valid reference controls; exact counts are printed by the runtime. Results are from executed originals, NOT Unity/PhysX or a mocked bridge. A separate new adapter should be tested by B01-61 at **its own new exact commit SHA** and accepted only with actual native Unity G1 where appropriate.

## Commands

```sh
dotnet run --project Tools/B01/B01-61/P17W15CrossProcessGenerationQA/CrossProcessGenerationQA.csproj -c Release | tee qa_test.log
python Tools/B01/B01-61/P17W15CrossProcessGenerationQA/receipt.py --log qa_test.log --out dist/b01-61-w15-generation-qa.json
```

GitHub Actions runs actual Windows2025+Ubuntu24.04 .NET8 and records exact original Git blobs, 7 new QA-only file hashes and full output. Only scratch temporary OS save files are created; no canonical scene, no game repository writes except this isolated QA branch. No Unity Editor XML/PhysX is claimed. `G0_SELECTED_556_SOURCE_ONLY / OFFICIAL_BRIDGE_SOURCE_QA_EXACT_SHA_WITH_RED_WITNESS / NATIVE_G1_NOT_RUN / SAVELOAD_ACCEPTED_HOLD / G2_HOLD / CODEX_ACK_NONE / NO_MERGE`.

**Next receiver:** B01-23/62 bridge author must provide real adapter exact SHA with an owned public API for real Unity/ledger transactional staging and durable commit authority. B01-61 independent tests that exact source, B01-40 validates safe staging and rollback, B01-60 executes native Unity. New QA branch is disposable and can be abandoned without touching gameplay.

## Official B01-23 PR #585 exact original host bridge — NEW independent RED witness

**Official producer** [Draft PR #585](https://github.com/C8LyNn/RoboRougelike/pull/585), exact immutable version tested `135a67d42acfccd59ca9145e09630d0b4bbade1c`, real `AtomicColdUnityHostBridge.cs` Git blob `e9e9dc6d437e96dad6b08c1639e8bd94d62c4f4b` and real Unity fixture `IsolatedSecureV2SaveHost.cs` Git blob `980fddbefbe17f41590e36efaab9b9df017660fe`. The official bridge is independent from the earlier unselected exploratory `B0123UnitySaveBridge001` branch; these sources are **never mixed or installed together**.

CI fetches the **exact original official source Git bytes**, directly compiles C#9 `AtomicColdUnityHostBridge.cs` + the genuine frozen #556 Save codec + original W06 and executes **13 independent adversarial transaction assertions** using test-only `IIsolatedRigFactory/IIsolatedRigWorld` failure injection. Original producer source is not modified; game #510 and Save #556 are unchanged. The test world is an intentional bounded interface substitute, **not a real Unity Rigidbody**, so it cannot assert G1 or game integration.

**Novel source release bug being tested:** original official bridge validates `staged.Capture` **before** calling `staged.SetVisible(true)`, but publishes the staged world without another identity/ledger/pose validation **after** activation. A legal `IIsolatedRigWorld` test implementation changes its captured state upon activation (plausible from Unity lifecycle callbacks/physics). It passes initial validation, becomes authoritative, and `TryLoad` reports `Accepted`; a later `TrySave` rejects its post-activation state. A successful **RED witness** proves the source-level interface cannot guarantee transaction atomicity across activation without a post-activate check/controlled rollback. This does not prove real Unity PhysX executes the mutation; native G1 is separate.

Additional positive and negative cases cover: valid Light/Heavy W06 ID cold load, original binary save, invalid digest/wrong run/stale rejection, second-idle refusal before commit, factory exception, staging identity rejection/cleanup, activation exception compensation, rollback compensation failure/quarantine, nested load denial and same-generation divergent commit acceptance. Both same-generation fork and cross-process older-generation replay require durable host transaction authority beyond current portable #556.

**Precise reproduction:**

```sh
mkdir -p dist/bridge
git show 135a67d42acfccd59ca9145e09630d0b4bbade1c:Assets/B0123SecureV2HostBridge001/Runtime/AtomicColdUnityHostBridge.cs > dist/bridge/AtomicColdUnityHostBridge.cs
git show 135a67d42acfccd59ca9145e09630d0b4bbade1c:Assets/B0123SecureV2HostBridge001/Runtime/IsolatedSecureV2SaveHost.cs > dist/bridge/IsolatedSecureV2SaveHost.cs
dotnet build Tools/B01/B01-61/P17W15CrossProcessGenerationQA/WorldSwapSourceQA.csproj -c Release -p:BridgeSourcePath=../../../../dist/bridge/AtomicColdUnityHostBridge.cs
dotnet Tools/B01/B01-61/P17W15CrossProcessGenerationQA/bin/Release/net8.0/WorldSwapSourceQA.dll | tee qa_bridge.log
python Tools/B01/B01-61/P17W15CrossProcessGenerationQA/receipt.py --log qa_test.log --bridge-log qa_bridge.log --bridge-dir dist/bridge --out dist/b01-61-w15-generation-qa.json
```

**Gate:** `SOURCE_ONLY` exact SHA, `OFFICIAL_HOST_ACTIVATION_ATOMICITY_HOLD` if RED witness reproduced; existing #510/private `AtomicDockLedger` lacks an authorized read/write adapter; no real canonical game Save/Load. `UNITY_G1_NOT_RUN / G2_HOLD / CODEX_ACK_NONE / NO_MERGE`. Receiver: B01-23 new separate versioned source correction, B01-62 independent ID/host check, B01-40 safe delivery, B01-60 actual activated Unity 6000.3.14f1 PhysX.
