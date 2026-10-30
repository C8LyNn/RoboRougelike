# B01-61 / PASS17 WAVE17 — exact **selected #596** Unity MonoBehaviour + checkpoint authority QA

**Final owner selection:** B01-23 [#611](https://github.com/C8LyNn/RoboRougelike/pull/611) and [#613](https://github.com/C8LyNn/RoboRougelike/pull/613) **chose only [#596](https://github.com/C8LyNn/RoboRougelike/pull/596) @ `efb2b06eafedab5469b57f163152d96aca3c5915`**. Rival [#604](https://github.com/C8LyNn/RoboRougelike/pull/604) SHA `1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a` is **ALT_HOLD, never install/merge/copy its source**. Prior standalone W17 QA [#616](https://github.com/C8LyNn/RoboRougelike/pull/616) was closed without merge immediately upon owner selection, and cannot certify the selected adapter. Frozen game [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) `d94400d95d13db7f27e1533df05c9292d5380431`, only Save contract [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) `33094f154c7b91119b36d58aab285e6e83c5de57`, original W06 remain byte-unchanged.

## Distinct new W17 evidence beyond prior B01-61 [#607](https://github.com/C8LyNn/RoboRougelike/pull/607)

#607 directly compiled producer **CORE** `AtomicWorldSwap.cs` and original #556/W06 with fake `IWorldFactory`. Here this new QA compiles/executes **the actual selected #596 `IsolatedTwinRigSaveHost.cs` Unity MonoBehaviour** (original Git blob `4bac6f327c9b93ae98dd2061c0ca8ea2100aab42`) **together with the actual producer core** Git blob `1f9e883c38101f214e4335867faa731f7ae67689`, unchanged selected original #556 codec/W06 source and frozen #510 stable rig spec. No runtime algorithm cloned, no new Save schema. The only test double is `SyntheticUnityEngine.cs`, emulating a **small managed GameObject/Transform/activeInHierarchy/Rigidbody API**, not Unity's actual native engine, collision solver, or PlayMode.

**EIGHT separate CLR processes PER OS** (1 physical source test, 1 simultaneous-host source safety gate, and 6 actual disk fresh-process checks) on the **EXACT original producer #596 SHA**:
- `physical`: four Light/Heavy attached/detached permutations, original Unity-facing MonoBehaviour `TryRestoreBytes` and `TrySaveBytes`, original W06 consumed IDs and source-selected frozen #510 numeric identities, two original `Rigidbody` components active in synthetic hierarchy, next Save generation (+1), wrong run/corrupt bytes/same-gen fork rejected, later gen commits, refuses coexistence with explicitly named legacy `SecureDebrisPlaygroundV2`.
- `concurrent`: **NEW security/ownership negative**. Two separate actual `IsolatedTwinRigSaveHost` components on distinct GameObjects, same run and real sidecar path, both pass `PhysicalIdle`; one publishes Light detached gen10, the other Heavy detached gen11 while the first is still ACTIVE. Old host's original `TrySaveBytes` reads durable gen11 and emits **GEN12 with its stale Light state**, and a third host accepts it. Old/current/new Light vs Heavy W06 consumed ledgers are split despite increasing generation. This is **source gate RED** for single-active-writer authority; persistent file lock is only held during I/O and does NOT lease scene host ownership. Authored exact original code, no reflection on game #510 private data. Note demonstration is synthetic Unity world hierarchy, **not measured PhysX**. Required B01-23/62 owner fix new immutable source version: enforce one authoritative host/slot lock for full lifetime or reject stale host reads/saves using exact committed world fingerprint/epoch, plus Unity native cases. No frozen #510/#556 changes.
- `seal`: actual original selected Unity MonoBehaviour writes authoritative `FileCheckpointStore` sidecar after gen7 Load; original public Save produces strictly newer gen8 binary on scratch disk.
- `restart`: brand-new CLR process reads actual saved original #556 binary with fresh Unity MonoBehaviour and original W06 consumed ID, commits gen8 to real sidecar.
- `fork`: different same-run/gen8 binary and older gen7 denied before scene-world publication.
- `advance`: real sidecar advances gen9 Heavy consumed.
- `stale`: brand-new process refuses previous gen8 Save bytes after gen9.
- `corrupt`: scratch-only sidecar corruption makes original core return immediate `RollbackQuarantine`, no physical world staged; Save blocked.

## Truthful gates and rollback

Passing security witness CI means **RED correctly detected**, **NOT safe release acceptance**. The selected #596 [#607](https://github.com/C8LyNn/RoboRougelike/pull/607) source core remains independently GREEN for its own scope; this tests **previously untested PUBLIC Unity-facing MonoBehaviour** and global host authority. Any new source fix must be owner-authored and independently re-tested exact new SHA. We refuse any competing #604 code in selected branch. Only **7 new B01-61 QA files**, pinned **5 exact original Git blobs**, no selected producer/Assets/frozen or Codex edits. Our test-only fake MonoBehaviour `Awake` dispatch uses reflection to simulate Unity lifecycle **only**, not to touch actual frozen #510 `Rig[]` or `AtomicDockLedger`.

**NATIVE G1 = NOT_RUN** (no licensed Unity 6000.3.14f1 real Editor/PhysX/NUnit XML/Editor.log), original demo's reconstructed W06 ledger is **NOT** frozen canonical #510 private R2/rig-world hydration, so `LIVE_GAME_SAVELOAD_HOLD`, Codex ACK NONE, G2 HOLD. Full Save + sidecar crash-atomic writer and multi-host authority NEED owner approval and real tests; `FileCheckpointStore` alone is not full crash-atomic snapshot commit. Formal physical source G0, GUID/asmdefs/reconstructed original owner ZIP SHA, no duplicate #604, safe disposable receiver/rollback belong B01-40+62, see #536/#575; NEVER original unsafe #510/#532 cleanup on shared targets. B01-00 alone controls #79 eventual OFFER; no merge.

**Manual reproduction:**
```bash
python Tools/B01/B01-61/P17W17Selected596UnityHostQA/source_guard.py
dotnet build Tools/B01/B01-61/P17W17Selected596UnityHostQA/Original596UnityHostQA.csproj -c Release --warnaserror
dotnet run -c Release --no-build --project Tools/B01/B01-61/P17W17Selected596UnityHostQA/Original596UnityHostQA.csproj -- physical ./temporary-scratch
dotnet run -c Release --no-build --project Tools/B01/B01-61/P17W17Selected596UnityHostQA/Original596UnityHostQA.csproj -- concurrent ./temporary-scratch
# Follow with seal, restart, fork, advance, stale, corrupt against the same scratch root.
```

**Owner receivers:** B01-23/62 correct final #596 W17 source (not #604) and document global slot-owner lease; B01-40 formally accept only exact original #596 ZIP with safe rollback; B01-60 native #532/#567/#581/#601 if activated otherwise NOT_RUN; B01-00 Codex/private #510 API authority. Report to [HQ #608](https://github.com/C8LyNn/RoboRougelike/issues/608).
