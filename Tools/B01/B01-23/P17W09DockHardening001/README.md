# B01-23 — PASS17 W09 | SAFEDOCK-HARDENING-001 v1.0.0

## Real safety correction, versioned without overwriting frozen sources

**Concrete defects found by source review:** frozen W07 [#439](https://github.com/C8LyNn/RoboRougelike/pull/439) has an **unguarded Reattach IMGUI button**, while frozen W08 [#455](https://github.com/C8LyNn/RoboRougelike/pull/455) uses a configurable `collisionLayers` mask for `Physics.OverlapBoxNonAlloc` which can omit physical blockers. Both are **real source-level unsafe pathways**; no licensed native PhysX reproducer yet. Do not edit #439/#455: this is **new W09 additive v1.0.0**.

New actual Unity code:
- `HardenedDockDemo.cs`: isolated, opt-in game component on **W07 demo object**, requires W07+W08, arms their existing root, disables the two original MonoBehaviours' `OnGUI` callbacks, and provides the **only intended protected demo UI** (Detach/SAFE reattach/Reset) without altering original `DebrisRigPlayground` methods. Disables old GUI if re-enabled on Update; fail-closed when it detects legacy UI is enabled. A separate `Physics.OverlapBoxNonAlloc(..., ~0)` always scans **all physics layers**, with per-object `Physics.ComputePenetration` on real native colliders, denies non-convex/unsupported shapes, >5mm penetration, query saturation, and parts farther than 3m. Runs original W08 `Inspect` as **second** gate, and invokes original W07 `Reattach` only when both agree.
- `HardenedDockPolicy.cs`: original C#9 source final gate with deterministic result codes. Critically **layers 0 or partially filtered masks never grant reattachment**.
- `HardenedDockInstaller.cs`: Unity Editor menu `B01 / PASS17 / W09 Install protected docking demo`, only attaches to named original isolated demo if W07 and W08 are present, via Undo; refuses duplicate. No canonical R1/R3/R6/Garage/CHL/PARTSIZE asset edit.
- `W09ProtectedDockPlayModeTests.cs`: 7 authored real Unity PlayMode tests including a BoxCollider on physics layer **8**, while forcibly setting original W08 layer mask to zero, must block without consuming original detach ID; moving obstacle must allow reattach, original GUI disabled, re-enable/tamper rejected, distance/identity guards. **NATIVE UNITY NOT_RUN** without licensed Unity6; source success is not a PhysX verdict.

### Exact frozen dependencies

- W08 [#455](https://github.com/C8LyNn/RoboRougelike/pull/455) commit `99dfbd25d471f9983f9472b12d3725f4c002f7e9`; original W08 `DebrisSafeReattachGate.cs` blob `7c7e96a10d69149119309863a0c2a9bdab829072` and `DockClearancePolicy.cs` blob `de0a847dbecacb92ad63a09ca12f5c9c372d597a`.
- W07 [#439](https://github.com/C8LyNn/RoboRougelike/pull/439) commit `cdcaea750fda1a9d7db6018e34970f4b9f206427`; original demo blob `e4e31c32cb5abda2fef987efa4b45fd61e828312`.
- W06 [#421](https://github.com/C8LyNn/RoboRougelike/pull/421) commit `cb2b3a76e84266e2730fffc4ab048c1039f0652f`.

### Test original real source + physical delivery
```sh
dotnet run --project Tools/B01/B01-23/P17W09DockHardening001/HardenedSourceTests.csproj --configuration Release
python -m unittest discover -s Tools/B01/B01-23/P17W09DockHardening001 -p test_package.py -v
python Tools/B01/B01-23/P17W09DockHardening001/package.py build --archive dist/B0123_W09_DockHardening_Delta.zip
```

Direct original `HardenedDockPolicy.cs` **and exact original frozen W08 `DockClearancePolicy.cs`** compiled as C#9/.NET8; no mock of the safety policy. Dedicated 64-combination source truth table + positive/negative and original-W08 exposure proof; native MonoBehaviour/PhysX requires Unity. Actual Git-original deterministic **W09 delta-only ZIP**, original source SHA, parent Git source blob pinning, 12 new unique Unity meta GUIDs and three isolated asmdefs, per-original-blob SHA256/bytes manifest; full ZIP corruption, false ACK/native PASS and safe external stage/rollback tested.

Staging/rollback only **new external disposable directory**:
`python Tools/B01/B01-23/P17W09DockHardening001/package.py stage --archive INNER_ZIP --destination ABSENT_EXTERNAL_DIR`
`python Tools/B01/B01-23/P17W09DockHardening001/package.py rollback --archive INNER_ZIP --destination SAME_DIR`
No source repo install or recursive dirty-target deletion.

### Safety/QA boundaries

This adds a **protected isolated demo UI only**. It cannot prevent external scripts from calling the original public W07 `Reattach` method, and re-enabling legacy components in the same frame remains outside a security boundary; canonical host integration must migrate all reattachment callers to an approved single guarded API. No real game gameplay/PhysX claim, no Codex ACK. The demo algorithm assumes W07 simple axis-aligned box and stationary owner; full compound vehicles/rotated sockets still unsupported. **Receiver B01-40** independent inner ZIP/GUID/asmdef/source importer, then **B01-60** independently run actual licensed Unity6000.3.14f1, seven genuine native PlayMode cases (including different layer blocker) and physical contact clearance; **B01-00** sole later Codex inbox. `SOURCE_G0_PENDING/UNITY_NATIVE_G1_NOT_RUN/G2_HOLD/ACK_NO/NOT_INTEGRATED`. No merge.
