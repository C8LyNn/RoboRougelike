# B01-21 — W14 / RC2 native map replay + clearance reception
Scope: exclusively `Assets/B01Packages/MAPGENWORLD001/**`, `Tools/B01/MAPGENWORLD001/**` and own docs/workflow. This is a **test fixture update**, not a game integration, generator rewrite, or unapproved upgrade to RC2. Keep frozen upstream MAPGEN R3.4.1 `5be31039411f89f17654b1c332f8692883fa03cc` and additive adapter W13 v1.1.0 [#261](https://github.com/C8LyNn/RoboRougelike/pull/261). Current RC2 [#258](https://github.com/C8LyNn/RoboRougelike/pull/258) uses old adapter v1.0.1 and must not be replaced by this task.

## What was added (REAL source)
- `MapWorldW14NativeAcceptanceTests.cs` — **12 individually-named Unity PlayMode tests**: `CombatArena` / `ObstacleCourse` × seeds **7/31/83** × actual measured width **Light 2.980m / Heavy 5.180m**. Uses the untouched producer builder, actual Unity colliders, `Physics.SyncTransforms`, one `WaitForFixedUpdate`, and `Physics.ComputePenetration` for an **unpowered rigid BoxCollider footprint (height 1m, known reference length, and an additional required 0.25m lateral clearance per side)** sampled 9 times per centreline segment, penetration threshold ≤5mm. On native successful generation obtains receipt, verifies bad-checksum replay never loses original GameObject/authoritative MapGenBuilder.Current or callback counts, then verifies legitimate deterministic replay and old-root deactivation.
- Separate **one Unity PlayMode negative test**: after valid Arena7, try impossible 10m Course seeds 2/12/14/22/28 with measured Light2.980m, require fail-closed with the existing arena/root and no premature clear/ready events. No silent seed rerolls. Tested code does not manipulate RC2 host, garage, salvage, currency, player HP or scene persistence.
- Executable **six source-level test checks** + **machine-generated native test matrix JSON**: prevent test-name duplication, missing width/seed, accidental claim of real R6 driving, missing corrupt-receipt rollback assertions, loss of prior Unity test coverage, missing Unity .meta GUID. This checks the suite's *structure* only and is not native Unity/PhysX PASS.

## Evidence boundary
- These tests **must be executed in licensed Unity 6000.3.14f1/URP 17.3.0** on a fresh disposable checkout after G0. `Physics.ComputePenetration` is truly Unity PhysX **only then**, not when Python scans the code.
- The footprint proxy has no wheels, support points, momentum, exact current robot height, attached parts, damage or steering. A pass would show merely **geometric overlap absence along the protected primary waypoint path**, not full physical R3/R6 traversal; it does not assert optional Heavy side-ramp safety. A native failure is a real blocker to investigate per seed, but not automatically proof that the full robot cannot bypass a collider with steering.
- Legacy producer R3.4.1 #241 external Codex native 43 NUnit and 12 API-steered actual R6 routes PASS, **not this W14 replay fixture execution**. Existing W13 adapter 7 Edit +5 Play native NOT_RUN in B01-21.

## B01-60 / Codex minimal, ordered disposable run
1. Freeze and print exact game test-target SHA and adapter source HEAD. Independently verify source files, .meta GUID collisions, and full source ZIP+manifest SHA256 against this PR Actions receipt. Never mutate user/active worktree. Preserve `Assets/B01Packages.meta`.
2. In new Unity6000.3.14f1 disposable checkout with full #237 and W13 #261 stack, run native compile and **7 authored EditMode + (5 existing +13 W14) = 18 PlayMode**. Collect unaltered original NUnit XML, Editor.log, exit codes, source SHAs and per-case messages. Run `MapWorldW14NativeAcceptanceTests` separately if a full suite failure interferes.
3. Check all twelve names and one 10m-negative in original XML, **no skipped/inconclusive tests**. Only if native all pass can report `W14_COLLIDER_PROXY_PASS`; still **R6_DRIVE_NOT_RUN**. Independently exercise real canonical R3/R6 vehicle on same seeds/rigs with gravity, wheel contact support, observed finish and collisions (not transform teleport) before claiming physical route acceptance.
4. For corrupted/invalid replay, confirm root identity, MapGenBuilder.Current identity, callbacks, state of canonical world remains unchanged. On success compare captured hash, receipt, seeded checkpoints and deactivation of old root. End with `ClearWorld` no leaked geometry.
5. Retest RC2 #258 separate at frozen source only after written HQ authorization for v1.1 install. Explicit no G2, no merge, no direct active Codex worktree edit, no off-scope ENV/PROJECT/Pack/BUILD/CAMERA source edits. Return any test failure with recipe/seed/rig/checkpoint/sample/collider overlap depth, original NUnit XML and log to B01-21 owner.

## Run source and package checks (safe CI)
```bash
dotnet run --project Tools/B01/MAPGENWORLD001/MapWorld.SourceTests.csproj -c Release -- /tmp/w14-policy.json
dotnet run --project Tools/B01/MAPGENWORLD001/Adapter.SourceTests.csproj -c Release -- /tmp/w14-adapter.json
python3 -m unittest discover -s Tools/B01/MAPGENWORLD001 -p 'test_*.py' -v
python3 Tools/B01/MAPGENWORLD001/test_w14_native_acceptance.py manifest /tmp/w14-native-matrix.json
python3 Tools/B01/MAPGENWORLD001/package_world.py build /tmp/w14-source
python3 Tools/B01/MAPGENWORLD001/package_world.py verify /tmp/w14-source/MAPGENWORLD001_SOURCE_FULL.zip
```
(Second dotnet invocation requires generation of `mapgenworld-evidence/adapter-shim/UnitySurrogate.cs` from existing build_adapter_shim.py before running; the project's CI does this.)

**Native Unity: NOT_RUN by B01-21 until genuine XML/log/exit. Independent B01-60: PENDING. B01-00 G2: HOLD.** Source QA receipt + final package hashes belong to completed Actions, not to this documentation's creation.
