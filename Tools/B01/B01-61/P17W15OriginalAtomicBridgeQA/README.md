# B01-61 PASS17 WAVE15 — direct original B01-23 new bridge source regression

This is the **independent acceptance QA on the actual NEW source** authored by B01-23 under `b01-23/p17w15-isolated-unity-save-bridge-001`, original new bridge commit `cf7f2a155570f99c412585d5f09f8f97f9e83754`, original Git code blob `74e853601ecc7a5455d9bb466432ce7c027e0695`. It executes **original unchanged `AtomicWorldSwap.cs`**, not a surrogate or another standalone Save schema. Actual original `#556` C# `DetachedSnapshotContract.cs` blob `b52e511c370f5fca56c74029050844dc8cbb7f9a`, original W06 `DebrisImpulseCore.cs`, original frozen `#510` `AtomicDockLedger.cs` and `SecureRigSpec.cs` all compile directly in .NET8 C#9 warnings-as-errors. All originals remain pinned and unedited. Only **six new B01-61 QA-owned files** plus own test workflow (no other branches or Codex changes).

## Real producer-core integration coverage

Our `FakeFactory` implements only the producer's authentic `IWorldFactory`/`IPreparedWorld` port. Every Save, Load, publication, stage, rollback and quarantine action calls the **actual original `AtomicWorldSwap` methods**. FakeWorld substitutes **the physical world only**, making deterministic controlled faults possible without impersonating real Unity/PhysX:

- First valid new #556 Light-detached/Heavy-attached checkpoint constructs a genuinely staged original W06 cold ledger, publishes one abstract world, and saves original #556 encoded bytes with actual #510 stable IDs.
- Different **later** generation: deactivate old, prepare inactive candidate, activate, validate actual snapshot, commit once, retire old. Verify original consumed ID switches exactly as part state.
- Genuine original decoder and bridge fail-closed denials (corruption, stale generation, wrong run, busy before stage, throwing host callback, busy after staged preparation); old physical *abstract-world token* stays active, unretired and Save outputs same prior original W06 ID.
- Inject original-world factory/prepare throw, staged capture identity mismatch, partial candidate Activate throw, and old-world Deactivate throw; verify original source's exception compensation, candidate retirement, old reactivation and restored consumed ID, no partial logical publication.
- Inject **failed candidate Deactivate on rollback after partial Activate**; the actual producer sets sticky `Quarantined=true` and refuses further loads and saves. The calling Unity MonoBehaviour must translate the error to `RollbackQuarantine`. This source QA checks the core state, not fake Unity results.
- Reenter same `AtomicWorldSwap.TryLoad` during hostIdle callback; new source's busy gate must block nested load.
- **NEW source release-critical conflict:** two different, individually valid original #556 checkpoints with **same run ID and generation** are both `WorldSwapVerdict.Accepted`. This occurs at the *real new host bridge* level: the previous world is actually deactivated/retired and replacement published while generation remains unchanged, with consumed Light vs Heavy W06 ID switching. It is **one verified managed fake-world replacement**, not proof of effects on native Unity Physics. The producer's generation condition is `candidate.Generation < currentCold.Snapshot.Generation`, not stricter equal-generation identity enforcement. Marker: `REAL_ORIGINAL_BRIDGE_SAME_GENERATION_DIFFERENT_WORLD_RED=REPRODUCED`, `WORLD_BRIDGE_RELEASE_HOLD=SAME_GENERATION_CONFLICTING_WORLD_ACCEPTED`. A test job **PASS** here denotes an observed **release RED**, not acceptance.

**Decision:** B01-23/62 should require a new versioned owner-controlled bridge with a single authoritative checkpoint revision (reject different content at equal run/generation; exact identical replay may optionally remain idempotent, but must not duplicate world transitions). B01-61 will independently retest new exact producer SHA; do **not** edit frozen #510/#556 or source already handed off.

**Permanent limits:** B01-23's new `IsolatedTwinRigSaveHost.cs` is a physically isolated **new** twin-rig demo. It explicitly does **not** import original #510 private Rig[]/AtomicDockLedger by public API; uses a separately built W06 ledger and Unity primitive rigidbodies. Thus this is not proof that canonical Secure Reattach V2 #510 can load physical real game saves. Actual native Unity 6000.3.14f1 Editor/PlayMode/PhysX with #532/#567 is **NOT_RUN**, no XML/Editor.log claimed. Canonical game integration, collision/migration on real Codex receiver, power-loss-safe disk writer, durable journal and game SAVE/LOAD acceptance still HOLD. #510's original physical source/receiver rollback deletes foreign empty dirs (confirmed #536); use independent B01-40 guarded #536/#542 receivers ONLY, never the original on shared/occupied paths. Frozen #556 and #510 unchanged, #544/#549/#554 alternative HOLD; no merge/ACK.

Run:

```bash
python Tools/B01/B01-61/P17W15OriginalAtomicBridgeQA/source_guard.py
dotnet build Tools/B01/B01-61/P17W15OriginalAtomicBridgeQA/OriginalBridgeQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W15OriginalAtomicBridgeQA/OriginalBridgeQA.csproj
```

Receivers: B01-23/62 own producer bridge correction and one selected #556; B01-40 independent safe receiver G0 for exact NEW bridge, migration/foreign empty dirs; B01-60 real licensed Unity native G1 or NOT_RUN; B01-00 exclusive Codex handoff #79. `SOURCE_QA_BRIDGE_RED_CONFLICT / #510_SOURCE_G0_ONLY / #556_SOURCE_G0_ONLY / LIVE_SAVELOAD_HOLD / NATIVE_G1_NOT_RUN / G2_HOLD / CODEX_ACK_NONE / NO_MERGE`. Rollback: close this own isolated QA Draft/delete only six new QA paths.
