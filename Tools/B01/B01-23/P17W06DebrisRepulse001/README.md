# B01-23 / PASS17 WAVE06 — DEBRIS-REPULSE-001 v1.0.0

**Actual gameplay mechanic:** opt-in one-time outward physical impulse for a genuinely detached robot part. Original Unity C#9 source, not VFX or canonical vehicle changes. Not wired to Codex R3/R6.

The core computes (Newton-second impulse) = kg mass * clamped outward delta-v; uses real linear velocity, deterministic eight-way XZ fallback when centres coincide, stable positive part ID, and idempotent one-shot ledger with explicit reattach/new-run reset. Defaults: target 2.5m/s, cap 4m/s. Nonfinite/bad IDs/mass/velocity/position input rejected without consuming IDs. No collision-event auto trigger or changes to HP/inventory.

## Exact, executable tests
- dotnet run --project Tools/B01/B01-23/P17W06DebrisRepulse001/DebrisRepulseTests.csproj --configuration Release
- python -m unittest discover -s Tools/B01/B01-23/P17W06DebrisRepulse001 -p test_package.py -v
- python Tools/B01/B01-23/P17W06DebrisRepulse001/package.py build --archive dist/b01-23-debris-repulse-001-original.zip

The .NET8 executable compiles the actual original Unity package C#9 core (33 tests). Python separately verifies real Git-blob source SHA256, deterministic physical Unity source archive plus 12 adversarial/rollback/positive tests. Two operating systems run identical-source GitHub Actions, which publish an ACTUAL downloadable inner ZIP with embedded per-file SHA256/bytes manifest. The ZIP is not a native Unity test.

## Optional isolated import / rollback
B01-40 verifies the exact artifact ZIP SHA256, manifest and all five new Unity GUIDs against target. Only in a fresh Codex-authorized Unity 6000.3.14f1 checkout, import exactly the Assets/B0123DebrisRepulse001 subtree plus folder .meta. Add DebrisRepulsionEmitter as opt-in component and explicitly invoke TryApplyDetachImpulse(stablePartId,body,ownerCentre) once per real detachable Rigidbody. Do not call on every collision, OnDisable or arbitrary HP damage. Host owns stable original part IDs, true detach timing and Rigidbody. RearmAfterReattach only after actual reattachment, ClearForNewRun only for a new session.

For safe detached staging, call package.py stage --archive INNER_ZIP --destination EMPTY_EXTERNAL_DIRECTORY; rollback with package.py rollback --archive INNER_ZIP --destination SAME_DIRECTORY. Stage refuses a pre-existing destination or source repo writes; rollback refuses modified, missing or unowned files. To remove from optional Unity integration: remove only the opt-in host callback and this new isolated subtree on its import branch, never canonical R3/R6/CHL/PARTSIZE, VFX or economy.

## Truth and acceptance
B01-23 owner source tests must pass; B01-40 independent source/scope/GUID/ZIP acceptance pending. Native Unity Editor compile, PhysX/real Collider separation, EditMode/PlayMode/Player, native save/identity/economy and human QA all **NOT_RUN** until B01-60 has authorized licensed Unity. **G2 HOLD / CODEX SENT NO / ACK NO / INTEGRATED NO / NO MERGE**. This is a real proposed game mechanic with a physical delivery archive, not game integration.

Receiver: B01-40 -> B01-60 independent native QA -> B01-00 inbox decision -> Codex only after explicit ACK.