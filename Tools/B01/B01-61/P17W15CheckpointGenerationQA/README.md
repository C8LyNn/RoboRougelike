# B01-61 — PASS17 W15 | Disk checkpoint → real CLR restart → ID reconstruction → conflicting same-generation replay

**Purpose:** provide genuinely new independent W15 **checkpoint durability and logical revision-conflict QA** rather than repeating [#563](https://github.com/C8LyNn/RoboRougelike/pull/563) / [#570](https://github.com/C8LyNn/RoboRougelike/pull/570) (old #550 RED→GREEN / reentrant callback). This is an acceptance **gate for future Unity Save/Load host adapter**, not that adapter. Original [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) SHA `33094f154c7b91119b36d58aab285e6e83c5de57` is **the sole frozen Save contract**. Frozen gameplay [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) `d94400d95d13db7f27e1533df05c9292d5380431`, original W06 `cb2b3a76e84266e2730fffc4ab048c1039f0652f`. Alternative #554/#549/#544 and #511/#494 remain HOLD. Do not alter any frozen code or Codex workspace.

## Actual execution, NOT an in-memory clone

`Original556CheckpointQA.csproj` **directly links the unchanged original C# source**: exact #556 `DetachedSnapshotContract.cs` Git blob `b52e511c370f5fca56c74029050844dc8cbb7f9a`, original W06 `DebrisImpulseCore.cs` blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`, original #510 `AtomicDockLedger.cs` blob `b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9`, `SecureRigSpec.cs` blob `b680ed0ec969ac23203fa55eab7b90f8daa86e43`. **No fake Unity, cloned codec, or copied ledger**, only independent test code. Native Unity 6000.3.14f1 / PhysX **NOT_RUN**.

One CI job executes a sequence of **four independent OS processes** (not one in-process roundtrip):

1. `write`: real original #556 `SnapshotCodec.Encode` for Light detached and Heavy attached, `System.IO.File.WriteAllBytes` to an actual disposable binary checkpoint on filesystem; byte SHA256 receipt.
2. `read`: launch an independent, new .NET process, read the saved bytes from disk, **reconstruct the actual original W06 consumed ledger** via original #556 `ColdSessionHandle.TrySwap`; verify both exact frozen #510 stable IDs, ownership, asset/socket, parent, detached flag, consumed counts. Returned public snapshot array cannot mutate published state and no mutable `GetLedger` is available.
3. `collision`: load original real checkpoint then build a **different yet fully valid** checkpoint with **the same run ID and same generation** (Light becomes attached / Heavy detached). Production code currently uses `new.Generation < Current.Generation`, not `<=`, and does not enforce same-generation content identity: **both saves return Accepted**, silently changing published ID/consumed distribution without incrementing generation. Must log `SOURCE_SAVE_GENERATION_CONFLICT_RED`. This is a real, distinct **persistence consistency release HOLD** if generation is supposed to identify a unique committed revision; not physical Unity world corruption evidence.
4. `negative`: corrupt checksum, truncate, append data, wrong run, stale generation, busy host, hostIdle callback fault and state becoming busy between validation and publish. In each actual original #556 method, the preexisting logical `Current` and original W06 ID must remain reference-identical. Reentrant host callback must reject nested transaction. Independently exercise actual frozen #510 `AtomicDockLedger` original W06 zero-impulse detach plus apply-failure rollback/reconsume.

The test exits **0 only if all intended negative releases are reproduced exactly**; success is **QA witness PASS**, not `SAVELOAD_ACCEPTED`. Always `SAVE_GENERATION_CONFLICT_HOLD` until a **new versioned producer SHA** documents and enforces same-generation conflict policy, ideally *same identity/generation + different bytes = refuse*, optionally exact identical checkpoint replay may be idempotent. A stricter monotonic policy may reject all equal after first publication. Receiver must independently verify on new exact source SHA. Do not rewrite frozen #556 in place.

## Reproduce

```bash
python Tools/B01/B01-61/P17W15CheckpointGenerationQA/source_guard.py
dotnet build Tools/B01/B01-61/P17W15CheckpointGenerationQA/Original556CheckpointQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W15CheckpointGenerationQA/Original556CheckpointQA.csproj -- write ./w15-checkpoint.bin
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W15CheckpointGenerationQA/Original556CheckpointQA.csproj -- read ./w15-checkpoint.bin
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W15CheckpointGenerationQA/Original556CheckpointQA.csproj -- collision ./w15-checkpoint.bin
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W15CheckpointGenerationQA/Original556CheckpointQA.csproj -- negative ./w15-checkpoint.bin
```

## Safety and actual receiver

The disposable file uses `File.WriteAllBytes` solely as a **QA fixture**; it is **NOT** an authoritative crash-safe atomic writer, journal, fsync/rename host adapter or real user save. Producer-owned Unity main-thread capture and atomic fresh-world+private-#510-ledger hydration have **not been supplied** here. Thus physically installed Unity Save→Restart→Load **NOT_RUN**, G1 native PhysX **NOT_RUN**, Codex receiver **NOT_SCANNED**. A future B01-23/62 owner-approved new bridge must prove no partial Rigidbody/Transform/GameObject state, real GUID identity mapping, safe generation rules and exception rollback, then B01-61 reruns the separate exact bridge SHA test.

B01-40 owns safe target-aware migration and receiver rollback: **original frozen #510/#532 rollback deletes foreign empty dirs** [#536](https://github.com/C8LyNn/RoboRougelike/pull/536) / [#542](https://github.com/C8LyNn/RoboRougelike/pull/542), use guarded wrapper and locks only. B01-60 native Unity test [#532](https://github.com/C8LyNn/RoboRougelike/pull/532) + [#567](https://github.com/C8LyNn/RoboRougelike/pull/567) must be real Editor/PhysX or G1 NOT_RUN. B01-00 coordinates source-only #79 handoff; no ACK/merge. Rollback close this own unmerged QA Draft, discard six independent B01-61-owned paths only, no producer or Codex files modified.

**Gates:** `#510_SOURCE_G0_ACCEPTED_ONLY / #556_SOURCE_G0_ACCEPTED_ONLY / #556_DISK_COLD_LOGICAL_ID_QA / SAME_GENERATION_CONFLICT_RELEASE_HOLD / PHYSICAL_SAVELOAD_HOST_HOLD / UNITY_G1_NOT_RUN / G2_HOLD / CODEX_ACK_NONE / NO_MERGE`.
