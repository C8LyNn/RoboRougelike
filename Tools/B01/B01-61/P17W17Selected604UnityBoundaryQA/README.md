# B01-61 / PASS 17 — WAVE 17 | Selected #604 Unity MonoBehaviour source QA

**Exact immutable source target:** B01-23 [#604](https://github.com/C8LyNn/RoboRougelike/pull/604) original final SHA `1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a`. Author W16 #596 `efb2b06eafedab5469b57f163152d96aca3c5915` remains a **completely different alternative** — **NEVER MIX files**. #596 already has independent [B01-61 #607](https://github.com/C8LyNn/RoboRougelike/pull/607) original C# QA. We target #604 specifically because it has the more complete owner Unity physical package; **HQ/B01-23/62 must authorize one final selected candidate before actual G0**. Frozen game #510 `d94400d95d13db7f27e1533df05c9292d5380431` and only Save #556 `33094f154c7b91119b36d58aab285e6e83c5de57`, original W06 source unchanged.

## New genuinely independent source surface: production Unity MonoBehaviour, not only its core

[W16 B01-61 #603](https://github.com/C8LyNn/RoboRougelike/pull/603) executed exact #604 **core and FileCheckpointFence** (104/104 real source assertions cross-OS) with `IWorldFactory` double; it did NOT compile the actual MonoBehaviour/Transform/Rigidbody parts. This W17 PR directly compiles and executes **ALL THREE actual unchanged #604 production C# files**: `IsolatedTwinRigSaveHost.cs`, `AtomicWorldSwap.cs`, `CheckpointFence.cs`, plus actual untouched original #556 `DetachedSnapshotContract.cs`, W06 `DebrisImpulseCore.cs`, and frozen #510 `SecureRigSpec.cs`. The only test doubles are in `SyntheticUnityEngine.cs`: game-object hierarchy, component lifecycle, activeInHierarchy, Vector3(float), Rigidbody state, scene enumeration and private Application.persistentDataPath. **This is synthetic engine API compatibility/source logic QA; NOT Unity Editor, NOT genuine PhysX, NOT a native Unity PlayMode test, no editor XML.**

Seven **separate CLR processes per OS** exercise actual selected #604 public **MonoBehaviour** entrypoints:

1. **`physical`:** every 4 Light/Heavy attached/detached state combinations, actual selected `MakeFixture` frozen #556 binary, real `TryRestoreBytes`, staged GameObjects/parts/Rigidbody active on publication, original W06 consumed stable numeric IDs, original `TrySaveBytes` full roundtrip. Corrupted SHA256, wrong run, same-generation different-world and foreign `SecureDebrisPlaygroundV2` MonoBehaviour co-install are denied with unchanged scene/ID; gen+1 replaces/cleans old; cannot swap persistent slot after a world exists. Test-only fake hierarchy models `activeInHierarchy` but does not simulate physics, contact, scene loading or Unity transforms fully.
2. **`seal`:** original Unity public `UsePersistentCheckpointSlot("W17_Selected604_Fence")` opt-in on unique private scratch root, actual W16 file fence 88 bytes, original #556 ID and W06 reconstructed. Directly probes whether unmodified Unity physical float positions (`Vector3`) recoded as double cause a **different SHA256 at SAME generation**, making `TrySaveBytes` reject even immediately after restore; if so prints separate source/API RED. This is a potential **save usability/quantization** blocker, not a crash-safety claim.
3. **`restart`:** independent fresh CLR process+fresh actual MonoBehaviour reads identical original checkpoint and watermark, stable Light/Heavy consumed states and both Rigidbody components.
4. **`fork`:** different fully valid same-run+generation original #556 binary rejected, no physical world created; stale generation rejected too.
5. **`advance`:** higher generation W06 Heavy consumed, real persistent file advanced.
6. **`stale`:** new CLR instance cannot replay a previously valid checkpoint after generation advances.
7. **`corrupt`:** controlled one-byte scratch watermark damage causes immediate `RollbackQuarantine`; no GameObject/Rigidbody publish, Save blocked.

**NEW separate author-internal native-test contract RED** (source-only witness, not a native run): the original #604 authored native test `IsolatedSaveWorldNativePlayModeTests.PersistentFence_DeniesConflictingSameGenerationAcrossHosts` creates `"W16Native_" + Guid.NewGuid().ToString("N")` = **42-character** slot, yet original `IsolatedTwinRigSaveHost.UsePersistentCheckpointSlot` strictly **rejects slot length >32**. The native authored test expects `Assert.IsTrue(old.UsePersistentCheckpointSlot(slot))`: this cannot pass while both exact source blobs are unchanged. Test compiles actual public method and verifies rejection. **B01-23 should correct only NEW owner-owned native test input slot ≤32 or produce owner-versioned compatible policy, NEVER touch frozen game #510/#556.** Do not falsely claim real native Unity run.

## Source-receipt constraints

- 7/7 unchanged original Git blobs pinned (3 owner producer Unity/core/fence, 1 original native authored test for reference, 3 frozen sources); 7 added B01-61 QA/workflow files only, zero changes to `Assets/**`, frozen game/source, #596, or Codex-owned code. Own isolated branch is stacked solely on producer #604 SHA.
- GitHub Actions matrix `ubuntu-24.04, windows-2025`, strict C#9/.NET8 warning-as-error, exact code coverage/exit logs and raw artifacts. **Source QA PASS = correct execution of positive and RED witnesses; a SOURCE RED is RELEASE HOLD.**
- Migration/G0 belongs solely to B01-40/62 on the one selected exact source ZIP and safe receiver, including foreign empty dirs, symlinks, overlapping GUID/asmdef and dirty target negatives. Never original unsafe #510/#532 rollback on shared host.
- Actual original game #510 has private `Rig[]` and `AtomicDockLedger`, no owner-authorized capture+fresh-host hydration contract, and this new #604 only constructs its **own isolated** W06 ledger. Thus `LIVE_GAME_SAVELOAD_HOLD` regardless of synthetic Unity results. Full disk Save+watermark two-file atomicity, multi-writer isolation and crash recovery not established; editor G1 real PhysX `NOT_RUN`; Codex ACK none, G2 HOLD. Close unmerged Draft for rollback; NEVER merge without owner authorization.

## Local reproduction

```sh
python Tools/B01/B01-61/P17W17Selected604UnityBoundaryQA/source_guard.py
dotnet build Tools/B01/B01-61/P17W17Selected604UnityBoundaryQA/Original604UnityHostQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W17Selected604UnityBoundaryQA/Original604UnityHostQA.csproj -- physical
# seal restart fork advance stale corrupt on one disposable persistentDataPath
```

**Receivers:** B01-23/62 select/fix/pin only #604 or #596, B01-40 only one official G0 physical source ZIP and strict safe migration, B01-60 actual licensed Unity #532/#567/#581/#601 native EditMode/PlayMode/PhysX or honest NOT_RUN, B01-00 only authorized Codex handoff. Report to [HQ #608](https://github.com/C8LyNn/RoboRougelike/issues/608).
