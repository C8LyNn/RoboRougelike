# CI41-691-ORIGINAL-CROSSPROCESS-SLOT-001/R1 — PASS17 WAVE21

**Independent owner B01-41 (CI/CD & TEST AUTOMATION)** · W21 [HQ #687](https://github.com/C8LyNn/RoboRougelike/issues/687).

## Authority and exact source

- **Sole selected canonical API:** frozen [#681 exact `eed2e39636edfd8b8db754c426db452164cbf9d7`](https://github.com/C8LyNn/RoboRougelike/pull/681), owner-selected **new V2 descendant** [#691 exact `809f82575c638e1b4d2806b5f417bddab6c0871a`](https://github.com/C8LyNn/RoboRougelike/pull/691). **Competing #682 ALT_HOLD**, #685 historical original P0 source RED. The V2 protected original `CanonicalWorldGateway.cs` Git blob `704e774dedd5be5af26e0791fba87ecdb109ae2a` is compiled **directly from selected original producer Git**; never copied or modified.
- **Sole selected host R4:** frozen [#647 exact `09c1809b73324a6d31c13e5d0143de62ac1e3ca6`](https://github.com/C8LyNn/RoboRougelike/pull/647), original physical `FileCheckpointStore` Git blob `8fd1dd9a1bc870433809da8fba897ec924fa4113`. Owner's **new R5 correction [#690 exact `cc7f73541517a18f4479bb155534bd4832d9985e`](https://github.com/C8LyNn/RoboRougelike/pull/690)** is independent, NOT installed, pending B01-61 #670 recheck. Losing #652 ALT_HOLD; older #604/#644 ALT_HOLD.
- Frozen original canonical game #510 `d94400d95d13db7f27e1533df05c9292d5380431`, wire #556 `33094f154c7b91119b36d58aab285e6e83c5de57`, R2 host #596 `efb2b06eafedab5469b57f163152d96aca3c5915`, selected R3 #642 `22cec9888047994ae523cbe7bf1b0a483d64cec4` and original W06 immutable, no edits. New test PR contains only seven **ADDED** CI41-only files and no game/producer/other-agent changes.

## New executable independent test: REAL selected Gateway + original physical disk CAS

Unlike producer V2's private test port that models a disk with `FileMode.Create`, this new independent runner compiles together **exact original V2 `CanonicalWorldGateway.cs`** and **exact original R4 `AtomicWorldSwap.cs` including `FileCheckpointStore`** plus unchanged actual #556 `SnapshotCodec`, W06 `DebrisImpulseCore`, and frozen #510 `AtomicDockLedger/SecureRigSpec`, using strict .NET8/C#9 warnings-as-errors. Tests are against real OS files within a unique UUID-named scratch path; no copied store/codec/core.

**New independent #685 source negative tests:** Start with real physical original g10 Light-consumed checkpoint, cold restore true original frozen #556 and consumed W06 Light/Heavy IDs. Source V2 `ICanonicalRigFactory.BeginOwnedStage` returns tracked handle **BEFORE** `PrepareInactive` creates a partial private staging world and throws without returning any pointer. Test four cases:
1. Owner tracked stage cleanup succeeds: old Light g10 active, no false quarantine, orphan stage Retired, original physical disk still g10 and preview still exact old original #556 bytes.
2. Owner stage cleanup deliberately returns false while partly built staged world remains active: V2 same-call permanent `RollbackQuarantine`, old original g10 remains as containment, future Save Preview/Restore refused, no forged g11 durable commit.
3. Owner stage cleanup throws: same sticky quarantine and original durable g10 preserved.
4. Real original `FileCheckpointStore.Commit` writes durable g11 Heavy W06 ID; the external adapter throws **after disk success before ACK**. V2 same-call quarantines and old Light world cannot export; new Heavy retired. An independent fresh canonical gateway reading genuine original physical g11 rejects Light g10 and cold-restores Heavy g11. All bytes and stable ID semantics from exact frozen #556.

**NEW true original gateway cross-process CAS:** 8 physically independent OS directories per OS, each seeds a genuine original FileCheckpointStore slot with run451/g10. Launch **two completely separate CLR child processes per race, 16 actual external CLR workers per OS**. Each cold-restores the same g10 original #556 scene and reports READY; parent releases a filesystem START barrier only after both have live separately staged worlds. They race incompatible g11 snapshot/consumed-ID bytes through the *real* `CanonicalWorldGateway.TryRestore`→`ICanonicalCommitPort`→original physical `FileCheckpointStore.Commit`. REQUIRE **EXACTLY ONE** source Accepted, loser StaleGeneration or RollbackQuarantine; exact winner digest/epoch persisted, stale fork cannot rewrite winning original disk; no orphan original `*.tmp`. **This verifies original physical file CAS only; distinct concurrent gateways can both cold-restore g10 before a lease exists. This is deliberate demonstration that an EXTERNAL lifetime host lease is mandatory and not implemented by this gateway contract.** It must never be presented as proof of live-world split-brain prevention.

Original CI workflow runs source_guard exact Git SHA+blob + own-only 7 paths, **18 independent adversarial anti-mock source test cases per OS**, strict direct original C# compile, true subprocess race and cleanup suite on **Windows2025 and Ubuntu24.04**, preserves original stdout and receipt JSON. No UnityEditor.exe, no fake NUnit, no Codex merge.

## Test boundary, gates and handoff

Successful C# source QA tests only the selected **V2 abstraction, managed physical OS checkpoint CAS and fake ICanonicalStagedWorld test object**; **does not compile actual frozen #510 private Rig[]/AtomicDockLedger Unity adapter**, never calls genuine Rigidbody/PhysX/GameObject, does not prove externally supplied cleanup handle truthfulness, does not certify single crash-consistent original #556 Save payload + independent trusted monotonic backup watermark (B01-62 [#669/#680](https://github.com/C8LyNn/RoboRougelike/pull/669)), and does not make whole canonical game Save/Load playable. #647 component-only destruction original [#670](https://github.com/C8LyNn/RoboRougelike/pull/670) remains SOURCE release RED until independent owner R5 #690 is retested. **Native Unity 6000.3.14f1/PhysX G1 NOT_RUN**; original local manual opt-in B01-60 [#684/#667](https://github.com/C8LyNn/RoboRougelike/issues/667) is separate. Formal G0 selected V2 and R5 pending B01-40/62, G2 HOLD/Codex ACK none.

Receivers: B01-23/62 own selected #691 API/real #510 private world implementation, and R5 #690 host separately; B01-61 replay exact #685/670 on V2/R5; B01-62 durable trusted authority/backup rollback, B01-40 source/GUID/G0/disposable migration, B01-60 genuine native Unity, B01-00 sole future authorized integration/Codex source handoff.

**Six lifecycle fields** `IMPLEMENTED=YES / TESTED=PENDING_ACTUAL_CI / QA_ACCEPTED=NO / READY_FOR_INTEGRATOR=NO / INTEGRATED=NO / RELEASED=NO`, `SOURCE_G0=PENDING / NATIVE_G1=NOT_RUN / LIVE_SAVELOAD=HOLD / G2=HOLD / CODEX_ACK=NONE / INTEGRATOR_FEEDBACK=NOT_MEASURED`. No merge; safe rollback leave Draft unmerged or remove exactly seven CI41-owned test files. Physical executable deletes **only** unpredictable UUID-owned OS test directories; never shared user `Application.persistentDataPath` or other agents' foreign dirs.

Reproduction on own new branch:
```sh
python3 -m unittest discover -s Tools/B01/CI41/P17W21 -p test_source_guard.py -v
python3 Tools/B01/CI41/P17W21/source_guard.py --repo . --output ci41-w21-source-receipt.json
dotnet build Tools/B01/CI41/P17W21/Original691RealSlotQA.csproj -c Release --warnaserror
dotnet run -c Release --no-build --project Tools/B01/CI41/P17W21/Original691RealSlotQA.csproj
```
