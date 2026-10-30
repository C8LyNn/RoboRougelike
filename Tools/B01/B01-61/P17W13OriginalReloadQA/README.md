# B01-61 | PASS17 WAVE13 — original #510 Save/Reload state loss witness

**Priority:** independent source regression of exact frozen [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) @ `d94400d95d13db7f27e1533df05c9292d5380431` with exact original W06 [#421](https://github.com/C8LyNn/RoboRougelike/pull/421) ledger `cb2b3a76e84266e2730fffc4ab048c1039f0652f`. #510 is the **sole selected source**, #511 and #494 HOLD. Never change frozen #510 or Codex sources.

**Not another W11 transaction fuzz suite.** [#517](https://github.com/C8LyNn/RoboRougelike/pull/517) already proved original-source 167/167 per OS for transactional correction. [#527](https://github.com/C8LyNn/RoboRougelike/pull/527) found missing persistence from read-only source inspection and negative model fixtures. **This W13 QA additionally executes the real original C# Unity-target `SecureDebrisPlaygroundV2` and `AtomicDockLedger`, original W06 `DebrisImpulseLedger`, across before/after rebuild and reset state transitions.** Only UnityEngine/Rigidbody/Physics surface is synthetic from previously verified independent source [#517](https://github.com/C8LyNn/RoboRougelike/pull/517). Native real Unity G1 NOT_RUN.

### Actual original-code probe

- Create actual original Light+Heavy C# rigs; call actual `Detach` on both, set nondefault physical positions and linear/angular velocities, confirm real W06-ledger consumed stable IDs.
- Serialize actual **observed state** to JSON with `System.Text.Json`, parse same bytes to confirm the saved *QA observation* contains two unique positive stable IDs, detached flags, consumed flags and motion vectors. **This JSON is a QA-only observation, NOT a production Save/Load serializer or accepted new schema.**
- Construct fresh exact-source `SecureDebrisPlaygroundV2.BuildDemo` simulating a new instance after reload: IDs numeric value same, but **both returned to attached with ledger empty and motion/pose lost**. All 3 public routes on both rigs then return `NotDetached` without repair. Six read-only gate denials are expected safe behavior, but prove the original public API does not rehydrate a detached save.
- Rebuild same host through actual `ResetForNewRun`: original detached objects are destroyed and rebuilt attached; stable numeric IDs unchanged while detached/consumed state irretrievably lost.
- Attempt reset inside an actual atomic ledger transaction: original `ResetForNewRun` rejects while busy, physical object preserved, original W06 consumed ID reconsumed after rollback. Same-run valid detach/dock still works. This distinguishes **persistence gap (RED release gate)** from **safe same-run transaction (PASS)**.

The expected output `ORIGINAL_SOURCE_RELOAD_QA_PASS` **means the witness executed and proved the SAVE/REHYDRATION release gap**, NOT that actual save restore succeeds. Output **`SAVE_REHYDRATION_RELEASE_GATE_RED`** is mandatory to stop false acceptance. No native Unity, actual game save file, host migration, or Codex receiver access.

### Reproduce

```bash
python Tools/B01/B01-61/P17W13OriginalReloadQA/source_guard.py
dotnet build Tools/B01/B01-61/P17W13OriginalReloadQA/OriginalV2ReloadQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W13OriginalReloadQA/OriginalV2ReloadQA.csproj
```

### Receiver contract for new versioned Save/Load adapter

B01-23/62 must publish a **separate SHA** and a production load/restore API (not QA reflection) with schema version, stable part ID + owning rig ID, detached/attached status, consumed ID ledger membership, 3D pose+motion, exact source or supported migration metadata and one unambiguous committed checkpoint. Validation **before any physics or ledger mutation**: duplicates/missing/foreign IDs, source mismatch, malformed/nonfinite/truncated JSON, partial rig set, unknown schema, or load while `Ledger.Busy` must fail closed. Restore must be atomic for **both rigs** on any exception and preserve old physical+ledger state. A successful loaded detached rig must have exactly once consumed ID and permit one guarded real physics reattach; attached rig cannot carry a consumed detached ID. Save during mid-dock should HOLD unless a real transactional snapshot barrier exists. Test against actual original #510 ledger, not a cloned algorithm.

**Handoffs:** B01-23/62 new independent versioned schema, B01-61 re-run source regression against its **new exact SHA** when published, B01-40 formal receiver G0 and foreign-empty-directory rollback, B01-60 licensed native Unity G1 with #532, B01-00 controls #79. Original #510 frozen and source-G0 accepted via #521, but `SAVE_RELOAD_HOLD / G1_NOT_RUN / G2_HOLD / CODEX_ACK_NONE / NO_MERGE`. Rollback: close QA Draft/delete own branch, preserve all original source and historical failure receipts.
