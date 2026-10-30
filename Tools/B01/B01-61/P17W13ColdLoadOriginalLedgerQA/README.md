# B01-61 / PASS17 W13 — REAL original W06 + R2 ledger cold-restart identity QA

**Single selected producer**: [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) frozen `d94400d95d13db7f27e1533df05c9292d5380431`. Other #511 and #494 HOLD. This PR creates an **independent negative acceptance oracle for persistence**, not a producer adapter, new game schema, Unity/Garage modification, or a duplicate of #517 transactional 167-case harness.

## Exact reproducible W13 proof
The .NET8/C#9 project compiles unchanged **actual original W06** `DebrisImpulseCore.cs` + **actual frozen #510** `AtomicDockLedger.cs` by source Include. Independently exercises Light (ID 2310001), Heavy (ID 2310002), consumed-state loss across NEW `AtomicDockLedger` instances, original `TryReattach` failing closed with NO physical callbacks on cold load, and the dangerous temptation to reconstruct consumed ID using W06 `TryDetach(..., desiredSpeed=0)`: this can seed an ID without ever validating the actual module/ownership. Real fault rollback reconsumes original ID; reset inside busy transaction forbidden. QA-only serializable DTO with Light/Heavy stable IDs is a **test fixture, NOT production Save/Load**, used to exercise validation denial on duplicate/missing/negative/reordered IDs, wrong version/SHA and detached/consumed inconsistency. No physical Unity state is claimed.

This is **distinct from B01-62 #527** which uses Python fixture/source scanning: here exact original C# transaction + W06 ledger methods execute in the CLR and directly demonstrate cold-session loss of consumed IDs. GREEN on these assertions means that the **missing persistence contract is correctly detected**, **not** that Save/Load works. An eligible new producer B01-23/62 adapter must be versioned on a NEW SHA; R2 #510 remains immutable. New independent acceptance will require actual original adapter `Save`/serialize/load/reconcile/atomic rollback across real original ledgers, both rig IDs, corruption, checksums, and native Unity tests, rather than test fixture reconstruction.

## Reproduce
From repo root with Python 3.11, .NET8 SDK and Git:
```bash
python Tools/B01/B01-61/P17W13ColdLoadOriginalLedgerQA/source_gate.py
dotnet build Tools/B01/B01-61/P17W13ColdLoadOriginalLedgerQA/OriginalColdLoadQA.csproj -c Release --warnaserror
dotnet run -c Release --no-build --project Tools/B01/B01-61/P17W13ColdLoadOriginalLedgerQA/OriginalColdLoadQA.csproj -- --evidence
dotnet run -c Release --no-build --project Tools/B01/B01-61/P17W13ColdLoadOriginalLedgerQA/OriginalColdLoadQA.csproj -- --release
```
Evidence must return 0; `--release` returns **intentional 3 HOLD**. The dedicated Github workflow runs exact-source tests on Ubuntu24.04 and Windows2025, preserves raw logs and exit codes. Native Unity6000.3.14f1 **NOT_RUN**. Source actual-Unity-host acceptance remains #517; physical G0 packaging/rollback #521; B01-23/62 own save schema, B01-60 native #532.

## Release gates & ownership
`B01-61_ORIGINAL_LEDGER_COLDLOAD_WITNESS` != `SAVELOAD_ACCEPTED`. Original #510 G0_SOURCE only; persistent Save/Load **HOLD**, G1 native NOT_RUN, G2 HOLD, CODEX_ACK_NONE, no merge. Receiver can use only selected #510, never #511/#494; no edits to original producer/Codex or main/develop. This PR never writes any save, scene or game file. Rollback close Draft and remove its six QA-only new files.
