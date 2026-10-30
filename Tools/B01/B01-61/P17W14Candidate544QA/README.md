# B01-61 — PASS 17 / WAVE 14 | independent exact #544 original contract selection gate

**Purpose:** Run an independent *direct C#9 source* safety gate on [B01-23 Save candidate #544](https://github.com/C8LyNn/RoboRougelike/pull/544) SHA `e77529262b8f03c6e9f0e25b642038c351872044` before B01-23/B01-62 choose **one** schema (#544 vs #549). Previous [independent #550](https://github.com/C8LyNn/RoboRougelike/pull/550) proved versioned #549 source RED (post-publication mutable snapshot + exposed original W06 ledger), but was **not evidence against #544**. This W14 probe now provides **direct original-source proof on exact #544**, not a transplanted claim from #549. Selected gameplay [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) remains frozen at `d94400d95d13db7f27e1533df05c9292d5380431`; #511/#494 hold.

## Source and execution, not copied stand-ins

`source_guard.py` asserts original #544 producer Git blob `2dd3ee7f465a595ed42c2cbb6637200bb903185f`, then stages **actual unchanged Git source bytes** from frozen original W06 `DebrisImpulseCore.cs` SHA `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`, #510 `AtomicDockLedger.cs` SHA `b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9` and `SecureRigSpec.cs` SHA `b680ed0ec969ac23203fa55eab7b90f8daa86e43` into **ephemeral QA-only CI input**. Direct `dotnet build` C#9 links real original producer and true R2/W06 source (NO UnityEngine stub). No author source, live Unity game, canonical Codex, or frozen SHA changed. 6 own QA files total.

## Distinct W14 tested boundaries

- Four true direct source Save→Decode→Prepare Light/Heavy attached/detached combinations, exactly correct original stable rig IDs and original consumed W06 ledger membership; malformed/corrupt checksum, missing and duplicated IDs, missing rigs, wrong run, stale generation, pending snapshot, busy host and alternate #511 SHA are rejected. Original **R2 atomic rollback** injection restores consumed ID after a thrown physical callback.
- **SOURCE RED 1:** after successful `PreparedRestore`, the public `Snapshot.Parts` is a mutable array. Caller can change the already-validated committed record to a duplicate/foreign ID, while source-original R2/W06 ledger retains original consumed ID. `readonly` constructor arguments and clone of **input** array don't prevent **output** mutation.
- **SOURCE RED 2:** public `PreparedRestore.StagedLedger(index)` returns the **mutable original frozen #510 `AtomicDockLedger`**. A caller can invoke original `TryReattach(id, ()=>{},()=>{})`, consuming/rearming real W06 ID **without guarded physical dock**; accepted snapshot still claims detached+consumed.
- **CONDITIONAL TRUST BOUNDARY RED 3:** `TrustedManifest.Parts` is a public array; a receiver leaking the reference permits replacing the authority's Light catalog record. The actual serializer then accepts an arbitrary foreign stable ID by reading the compromised authority; original #510 canonical `SecureRigCatalog.Light.PartId` is unchanged. This is a conditional trust boundary hazard, not evidence that a properly isolated receiver manifest is externally writable.

**Success for THIS QA = the security risks reproducibly detected, not #544 Save/Load safe.** Log must print `CANDIDATE_544_SOURCE_RELEASE_GATE_RED`, and therefore a released candidate remains on HOLD. Producer #544 and #549 are different competing proposals; HQ/B01-23/62 must select a single new versioned immutable corrected contract and retest exact new SHA. The historical producer #544 per-OS source functional PASS is not invalidated for its limited positive tests, but external mutation security gate is RED. Frozen #510 remains unchanged and its `#521 G0_ACCEPTED_SOURCE_ONLY` stands.

## Reproduce

```bash
python Tools/B01/B01-61/P17W14Candidate544QA/source_guard.py
dotnet build Tools/B01/B01-61/P17W14Candidate544QA/Original544SecurityQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W14Candidate544QA/Original544SecurityQA.csproj
```

**Boundaries:** SOURCE QA only, no genuine Unity 6000.3.14f1 Edit/PlayMode/PhysX/Player XML; `NATIVE_G1=NOT_RUN`. No live Unity `SecureDebrisPlaygroundV2` private ledger hydrate bridge, no canonical Save/Load or host migration. B01-40 independent rollback #536 and #542 (old original destructive empty dirs RED; use new strict receiver wrapper), B01-60 existing #532 native evidence only when activated; otherwise NOT_RUN. G2 HOLD, CODEX source-only OFFERED (not ACK), NOT_INTEGRATED, NO_MERGE. Rollback close Draft/remove only QA branch. Receivers: B01-23/62 schema fix and exclusive choice; B01-40 formal new source G0/rollback; B01-60 native engine; B01-00 coordination.
