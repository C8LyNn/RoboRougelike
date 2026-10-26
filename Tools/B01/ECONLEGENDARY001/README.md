# ECONLEGENDARY-GUARD-001 / B01-22 W13 — cross-source gameplay safety patch

**AUTH:** B01-00 W13 #71 comment 6411869257. Owner: B01-22. Cross-module invariant FIX, not duplicate economy/inventory.

**Pinned inputs** (unmodified, verified by Git blob SHA before compiling):
- ECONREBUILD #252 `12572117a3dce369257feabc57021cde58023369` / actual source blob `cac95b28647528a7a46cb818c6d78037dc3e80a2`.
- LEGENDARY-UNITY #242 `dffabbc6960304fdb33322b5a23c884feac804c6` / actual source blob `b5ec9f163cd9e967e09a1876ed3f39412ff6c53c`.
- Reference OBJ R3 #210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8`, R3 authoritative Parts+Inventory.

Actual new **Unity C# runtime** `Assets/B01Packages/ECONLEGENDARY001/Runtime/GuardedLegendaryPort.cs` with asmdef references `B01.Legendary.Unity` + `B01.EconRebuild.Runtime`. Wrap host's existing `IAuthoritativeR3Port` with `GuardedLegendaryPort`; pass the guarded port into `LegendaryUnityService`. Does not create a second wallet/stock/ID ledger, nor bypass ECON's win-bound paid 0HP rebuilding.

**Real functionality and host requirements:**
- Verifies ECON's real live `OwnedParts` matches LEGEND's actual `Mounted + Inventory` by unique physical ID, kind, HP, before guarded mutation. ECON observation may contain additional nonlegend parts.
- Requires **explicit trusted authoritative** `IRebuildReservationProof` per physical instance; if unavailable FAIL CLOSED, never guess from absence in sidecar queue. Treat both ECON `IsRebuilding(id)` and actual reservation as blocking mount/unmount/repair/transfer/salvage.
- A destroyed **0HP legendary is never instantly repaired via the historical native R3 Repair**. Returns `REBUILD_REQUIRED`; if queued, `REBUILD_PENDING`. Once a **real next win** completes ECON batch and HP>0, the host can mount it normally, preserving the same instance GUID.
- Transfer requires BOTH parties guarded, authoritative proof, and original atomic R3 transfer support; no unguarded destination.
- Original typed socket checks still execute within LEGENDARY (WeaponBase/MobilityHub), and missing BUILD typed 3D/multi-cell validation remains blocked.
- Does **NOT** intercept a third-party caller invoking raw R3 Repair, ReceiveSalvage or Mount directly. Codex MUST route those entry points through controlled service and ECON reservation rules. Full host+two-sidecar checkpoint must be atomic or fail-closed. Whole-run lifetime/anti-softlock loaner remains HQ decision.
- Source results from portable .NET8 harness compile **actual immutable upstream C# sources** fetched at exact full commit and verified Git blobs; Unity JsonUtility stub explicitly labeled NON-NATIVE. This is stronger than reproducing policy in new simulated code, but **NOT** native Unity/PhysX/real host R3.

## W14 RC2 — CONFIRMED ROOT CAUSE / REQUIRED RECEIVER PATCH

The W14 **negative red test** used TWO SEPARATE R3 backing stores with an identical
physical GUID, kind and 0HP, one under LEGENDARY and one under ECON. Older
`CheckSharedOwnership` accepted the matching snapshots and could therefore
allow an ECON win to restore a DIFFERENT model from the one used in the garage.
See the pinned failing run for a real 18/19, not a theoretical warning.

**Fixed invariant:** both host ports MUST implement
`ISharedR3PhysicalOwnerIdentity` and return the exact same LIVE
`R3VehicleModel` reference in `AuthoritativeR3Model`. The guard checks
`object.ReferenceEquals` before any host mutation. Missing owner proof
`OWNER_IDENTITY_UNAVAILABLE`, different owner instance
`OWNER_IDENTITY_MISMATCH`. A matching string GUID, cloned R3 model,
equal-valued DTO or Unity instance ID is NOT identity proof.
**Do not silently fallback when proof unavailable.**

**Executable receiver bridge**: the W14 CI fetches the pinned original
`R3LegendaryPort.install.cs` (#242) and `R3EconRebuildPort.install.cs`
(#252) with exact Git blob checks, generates two adapted install-only .cs
files in `Tools/B01/ECONLEGENDARY001/_receiver/` WITHOUT modifying upstream,
then compiles both against the verified OBJ #210 *API signature stubs*.
The Actions artifact contains the two compiled-source receiver bridge files,
their SHA256 receipt and instructions; **not in the Unity Assets ZIP**.
Codex should use these exact generated .cs instead of the older unguarded host
examples, and instantiate them with ONE identical `R3VehicleModel` object.
The `IAtomicRebuildR3Hooks` hooks, certified typed sockets, real 3D
collision and durable wallet/run-event journal still require original
integration owners and explicit G2.

**Reproduction and native acceptance cases**:
`W14_NATIVE_ACCEPTANCE.md`. Portable C# tests prove the real source
path; the new authored Unity EditMode tests are **NOT_RUN** until a licensed
native Unity 6000.3.14f1 run with XML, exits and source identity.
No claim of integrated RC2 ECON/LEGENDARY gameplay.

## Reproduce
```bash
bash Tools/B01/ECONLEGENDARY001/prepare_upstream.sh
dotnet build Tools/B01/ECONLEGENDARY001/tests/CrossQa.csproj -c Release --nologo
dotnet run --project Tools/B01/ECONLEGENDARY001/tests/CrossQa.csproj -c Release --no-build -- artifacts/econlegendary/results.json
python3 Tools/B01/ECONLEGENDARY001/build_package.py artifacts/econlegendary
```

CI uploads deterministic Assets ZIP, source manifest, console and per-test JSON with pinned head. Native Unity EditMode/PlayMode/PhysX **NOT_RUN** unless verified with licensed editor. G0 B01-40 independent, G1 B01-60 independent, G2 HOLD.

## Minimal Codex install / rollback
1. Freeze EXACT source SHAs for #252/#242 and this Draft; validate all three manifests and upstream version pins, import both producer Unity packages into disposable isolated Unity6000.3.14f1 worktree, preserve canonical Assets/B01Packages.meta, then import this ZIP.
2. Bind `IRebuildReservationProof` to genuine durable R3/ECON reservation ledger; missing proof should reject *all* protected mutations. Bind ECON and LEG ports to the **same live R3** Parts/Inventory, not independent DeepCopy previews. Wrap LEG port with `new GuardedLegendaryPort(actualLegendPort, actualEconPort, rebuildService, proof)`.
3. Route garage repair, unmount, salvage, mount and transfer through guarded services and ECON queue, not unguarded native R3 public methods. Persist physical R3 state + ECON sidecar + LEGEND sidecar in one crash-consistent checkpoint, restore R3 -> ECON -> LEGEND in disposable game state before publishing.
4. Test 0HP inside run, one loss, one valid win and back-to-garage, replay victory, save/load crash injection and lost parts; test two robot owners, all available typed sockets/size/physics, avoid unapproved model masses.
5. Run licensed native Unity Editor/Play/Windows suite with B01-60 and independent B01-40 receiver preflight. No G2 or active Codex edit without B01-00.
6. **Rollback:** stop host mutations, backup saves, remove only ECONLEGENDARY001 Assets/meta and new guarded port wiring, restore original host event routing and preinstall R3 data, rerun baseline. Never delete ECON or LEGEND existing ownership, never silently repair all 0HP.
