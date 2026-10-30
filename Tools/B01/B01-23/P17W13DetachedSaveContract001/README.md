# B01-23 / PASS17 WAVE13 — DETACHED SAVE CONTRACT 001 v1.0.0 (NOT a new Secure V2 candidate)

**Frozen ONE gameplay source:** [#510 @ `d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510) `B01-23-SECURE-REATTACH-V2/2.0.0`. This package is a **new optional, isolated SERIALIZATION CONTRACT**, stacked read-only on #510; it does **not** change frozen #510's HEAD, source code, Unity scene/physics/ledger or Codex game. Alternatives #511 and #494 remain HOLD. The package does NOT become a replacement Secure Reattach controller.

## Identified real blocker

Independent B01-62 [#527](https://github.com/C8LyNn/RoboRougelike/pull/527) found no consumed/detached ID persistence in the frozen #510 controller: its `AtomicDockLedger` per Light/Heavy rig holds actual W06 `DebrisImpulseLedger` only in memory. Restoring a visual part without restoring that ID is unsafe. The W06 source is unchanged.

## Strict portable versioned Save/Load V1 contract

`Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs` is original C#9/Unity-compatible **no UnityEngine dependency**, references unchanged W06 `B0123.DebrisRepulse.Runtime` via one new isolated Unity asmdef. Binary format: fixed 8-byte magic `B01SV13!`; 32-bit **schema 1**; **exact frozen #510 40-byte ASCII source SHA**; 64-bit positive run ID and positive monotonic generation; exactly 2 **ordered** part records Light/Heavy. Each record stores stable part ID, stable owner ID, explicit parent ID, fixed 32-char logical asset GUID, fixed socket GUID (do NOT confuse with Unity `.meta` GUID), attached/detached enum, consumed ledger bit, kinematic bit, physical world position XYZ, normalized quaternion XYZW, linear and angular velocity XYZ, and mass kg. SHA256 checksum over entire exact payload appended; caps on bytes, values and numeric finiteness, exact EOF. SHA256 is **corruption detection, not an HMAC or protection against deliberate re-signing**; foreign/unsupported IDs, sockets, mass and states must still be rejected against frozen catalog.

**Crucial invariants:** `detached -> consumed=true, kinematic=false, parent=0`; `attached -> consumed=false, kinematic=true, parent=owner, velocities=0`. Reject unknown schema, corrupt/truncated/extended payload, duplicate/missing/swapped IDs, wrong rig owner/asset/socket GUID, alternative #511/#494 source SHA, invalid quaternion/NaN/Inf/position/velocity, mismatched mass or state/consumed bit. Wrong run or stale generation -> refuse load. A busy detach/dock or host idle probe failure -> refuse before touching CURRENT logical session.

**Rehydration order:** (1) verify byte length+SHA256 (2) verify schema+source SHA+all identity catalog refs (3) validate all rig records + no duplicates, parent and physics bounds (4) build NEW cold W06 `DebrisImpulseLedger` for each detached part using actual frozen `DebrisImpulseLedger.TryDetach(...zero impulse...)`, with checks `Contains/Count`; attached ledger is empty (5) only after both are built and idle guard passes, `ColdSessionHandle` switches the immutable **portable logical session pointer**. Reject invalid before pointer change. **NO Unity live Rigidbody/Transform/AtomicDockLedger mutation occurs in this package.** `ColdRestore.GetLedger` exposes cold pure-W06 restoration proof, but it is **not an injector into private #510 `AtomicDockLedger`**.

**RECEIVER REQUIRED:** Codex-authorized new Unity adapter must allocate a completely fresh V2 rig/world off-scene, hydrate physical bodies, mount parents and private authoritative ledger from the cold plan, then atomically publish both together while no detach/dock/reset is active. This requires an **explicit new versioned host extension or internal restore API**; frozen #510 has NO such import hook and **must not be monkeypatched/reflected into**. If implementation cannot atomically publish physical bodies+W06 IDs, gate = `SAVELOAD_HOST_HOLD`, not an accepted game save. Real live host G1, save/checkpoint/scene integration, cross-version migration all NOT_RUN/NOT_IMPLEMENTED. A logical session swap is not a Unity game swap.

## Actual executable tests and physical delivery

```sh
dotnet run --project Tools/B01/B01-23/P17W13DetachedSaveContract001/SnapshotSourceTests.csproj -c Release
python -m unittest discover -s Tools/B01/B01-23/P17W13DetachedSaveContract001 -p test_package.py -v
python Tools/B01/B01-23/P17W13DetachedSaveContract001/package.py build --archive dist/B0123_DetachedSaveContract001.zip
```

C#9 actual original W06 `DebrisImpulseCore.cs` and exact new portable serializer compiled with warnings-as-errors. Test executes **real filesystem binary write/read** using a fresh temporary scratch folder, all 4 detach/attached permutations, original W06 ledger rehydrate and release, wrong checksum, schema, selected SHA, detached/consumed mismatch, foreign GUID/owner/mass, busy/reset/old generation no-partial logical mutation, 128 deterministic mutations and roundtrips. No fabricated Unity I/O test.

Physical ZIP contains only 12 new W13 owner files + per-file original Git SHA/bytes manifest; source pins frozen #510 SHA and actual old W06 ledger + original #510 host/atomic/rig blob SHAs, 4 distinct W13 own Unity .meta GUIDs and one asmdef, no alternative game files. Source packager safe stage/rollback only **outside the repository** and refuses **foreign empty directories**, files, dirty bytes, symlink and partial stage. This independent external scratch utility is **NOT** the B01-40 authorized host rollback wrapper nor a fix applied to original #510 package.py.

**QA:** B01-62 independent schema, duplicate/corrupt/stale/per-run validation and cold ledger; B01-61 original #510 reattach transaction regression after restore simulation, no physical mutation when bad; B01-40 package physical blob/GUID/asmdef+foreign empty dir rollback independent acceptance; B01-60 run native #532 when licensed Unity exists, later native Save/Load once Codex-authorized host adapter exists; B01-00 sole future Codex #79 offer. `SOURCE_TESTED_ONLY / B01-40_G0_PENDING_FOR_NEW_CONTRACT / B01-62_SAVE_QA_PENDING / SAVELOAD_HOST_HOLD / UNITY_G1_NOT_RUN / ACK_NONE / INTEGRATED_NO / NO_MERGE`.
