# B01-61 — PASS17 W13 | exact new producer Save/Load contract source acceptance

**Target exact new versioned producer SOURCE SHA** `c2c179317af8155d17a1027283fe991565779ede` on `b01-23/p17w13-detached-save-contract-001`, **new original schema Git blob** `6c036985411164372bf3d21d467780ff3777544e`. **Frozen selected original gameplay #510** `d94400d95d13db7f27e1533df05c9292d5380431` and W06 `cb2b3a76e84266e2730fffc4ab048c1039f0652f` completely unchanged. This is a **source-only independent QA of the new logical cold Save/Load contract, NOT a real Unity host rehydration acceptance**.

Actual direct original C#9 project `OriginalProducerSchemaQA.csproj` links original producer `Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs` and original unchanged `Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs` from Git. No copied snapshot/ledger algorithm, no Unity stub (pure actual original source), no Codex code.

## Independent acceptance positives and negative controls

Runs actual original `SnapshotCodec.Encode/TryDecode` two-rig logical attached/detached roundtrip; validates stable #510 `2310001/2310002`, original W06 consumed ID rebuild, source `ColdSessionHandle.TrySwap` checks wrong run, stale generation, busy host, and callback failure quarantine. Negative cases: corrupt digest, null, undersized, oversized, truncated, reversed rig ordering, duplicate or missing stable ID, missing consumed flag, NaN position.

**Two independently discovered RED security weaknesses from actual producer API, not from copied fake code:**
1. `RunSnapshot.Parts` is a **public array**. Although its field reference is `readonly`, any holder of accepted `Current.Snapshot` can **replace elements AFTER validation/publication**, leaving an invalid logical snapshot while the original real W06 consumed ledger remains unchanged. Test calls actual code only.
2. `ColdRestore.GetLedger(slot)` exposes the mutable actual W06 `DebrisImpulseLedger`, allowing an arbitrary caller to execute `RearmAfterReattach` **without a physical docking transaction** after cold restore, so `Current.Snapshot` claims detached consumed ID when ledger no longer contains it. Test exercises actual W06 method directly.

These two witnesses produce `SAVE_CONTRACT_RELEASE_GATE_RED=MUTABLE_VALIDATED_SNAPSHOT_AND_EXPOSED_LEDGER` while the QA job itself succeeds, **because QA detected mandatory HOLD**. They are separate from historical frozen #510 / #517 (W11 safe same-run transaction) and original full-controller reload gap [B01-61 #538](https://github.com/C8LyNn/RoboRougelike/pull/538), which executes the unmodified real #510 Unity-target source with only synthetic Unity API. Do not accept these new logical schema checks as proof that the **actual #510 private AtomicDockLedger** was populated: the new adapter explicitly has no Unity host bridge yet.

## Repro

```bash
python Tools/B01/B01-61/P17W13ContractBoundaryQA/source_guard.py
dotnet build Tools/B01/B01-61/P17W13ContractBoundaryQA/OriginalProducerSchemaQA.csproj -c Release --warnaserror
dotnet run -c Release --no-build --project Tools/B01/B01-61/P17W13ContractBoundaryQA/OriginalProducerSchemaQA.csproj
```

Native Unity Editor / real PhysX G1 **NOT_RUN**; physical Unity Save/Load **NOT_RUN**; actual receiver Codex NOT_SCANNED; production release **HOLD**.

**Receiver:** B01-23 original producer (new corrected version SHA, never rewrite frozen #510/old cited version), B01-62 independent owner ID/save contract, B01-40 physical G0/foreign empty directory, B01-60 #532 native Unity if licensed, B01-00 sole #79 Codex queue. Fix options: hide internal arrays/ledger behind defensive immutable views or guarded operations, prohibit direct mutator exposure, ensure immutable accepted record and original #510 host atomic bridge before release. Retest corrected **new exact SHA** before approval. Rollback: close own unmerged Draft; no existing gameplay source changes.
