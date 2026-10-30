# B01-62 / PASS 17 — WAVE 13 / SAVE-ID-REFERENCE + ROLLBACK-FOREIGN-DIR-GATE 1.0.0

## Ownership / release scope

**Only selected game source:** B01-23 Secure Reattach V2 R2 **[#510@d94400d95d13db7f27e1533df05c9292d5380431](https://github.com/C8LyNn/RoboRougelike/pull/510)**, frozen and SOURCE_G0_ACCEPTED_SOURCE_ONLY by B01-40 #521. Alternatives #511/#494 HOLD. This independent B01-62 folder is a **QA reference contract / negative regression**, not a producer gameplay patch and not a canonical Unity Save/Load adapter. No source, Unity scene, Codex worktree, game save or #510 SHA overwritten. Historical #486 RED and #517 source GREEN still refer to their respective immutable versions.

## Contract v1 (reference, NOT a claim the host implements it)

**Envelope:** deterministic UTF-8 canonical JSON, `payload` plus lower-case SHA-256 of serialized canonical payload. SHA-256 provides accidental tamper/corruption detection, **not cryptographic authenticity**. The reference schema includes `schema`, exact frozen `selected_sha`, stable `run_guid`, nonnegative `generation`, `transaction_active=false`, `phase=idle`, and exactly two rig entries. Each entry contains immutable positive `id` and 32-lowercase-hex `part_guid` bound to the selected QA fixture catalog, boolean `detached`, original-W06-derived `consumed`, expected `parent_guid` or null, `kinematic`, position, rotation quaternion, linear and angular velocities. **All actual GUID values in this folder are TEST FIXTURE IDS, NOT Unity .meta GUIDs or approved persistent game IDs.** The game must define its own stable GUID assignment and collision strategy.

**Invariants:** `detached == consumed`; a detached body must be dynamic and have null root parent; attached body kinematic and parented to expected root; exactly one record for each selected part ID/GUID, no unknown fields, nonfinite vectors, invalid quaternions, version downgrade, alternative producer SHA, live detach/dock/reset, duplicate JSON keys, oversized/altered envelopes. Reject everything before touching an existing state. `restore()` validates and constructs a new detached in-memory state, runs optional failure injection, then performs a single swap in the **reference dictionary**. This is not physical Unity GameObject/PhysX transaction atomicity.

**Actual original ledger proof:** C#9/.NET8 `Program.cs` compiles **unchanged original W06 `DebrisImpulseCore.cs` from immutable #510 Git**. It accepts three Python-generated and prevalidated JSON fixture envelopes (both attached, Light detached, both detached), validates actual SHA-256 envelope, builds **fresh original W06 ledgers** and consumes IDs using actual `TryDetach` with **zero impulse**; verifies each consumed ID cannot detach twice and rearms exactly once, while attached parts remain unconsumed. This demonstrates a possible rehydration algorithm, not a method available in the game today; **native game state/SaveLoad remains HOLD until B01-23 versioned adapter + Unity G1**.

**Replay/load lifecycle proposed for B01-23:** (1) write Save snapshot only at *quiescent transaction boundary*; (2) validate full schema/hash/IDs/GUIDs and dependency/source SHA before mutation; (3) create an off-scene, new W06 ledger for each physical rig; (4) reconstruct consumed IDs without actual forces, then stage associated physical rigidbodies, transforms, parent and detach state; (5) reject any inconsistent/failed construction without altering existing player state; (6) commit complete staged state as one atomic receiver-controlled unit, otherwise rollback **all** body/ledger references with quarantine on rollback failure. Do not perform `ClearForNewRun` on an active detached rig while load/save/reattach executes; reset during transaction must be denied. Unity G1 must cover forced exception mid-restore, live reset, unloaded legacy scene, collision/reattach immediately after reload, and two different masses/IDs.

## Independent foreign empty directory original #510 RED→safe gate

`rollback_witness.py` creates a **disposable detached Git worktree of the actual frozen #510 SHA**, imports and EXECUTES the **unchanged original** `Tools/B01/B01-23/P17W10SecureReattachV2/package.py` to build the real 99,666-byte/35-member archive (SHA256 `d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387`), stages it to a new external temp directory, creates an unrelated **EMPTY** subdirectory and executes the original rollback. The expected **RED** is proof that original implementation removes it without refusing; no real user directory is ever involved.

The distinct **B01-62 receiver-only** `strict_rollback` additionally verifies the *complete recursive directory set*, not just files. It rejects foreign empty root/nested folders, symlinks, extra/changed/missing files, only delegates to original rollback when all members including directories are owned and byte-exact. Positive clean rollback and negative staged FOREIGN FILE/DIRECTORY preservation are executed with real original ZIP and the real original package module. This demonstrates a fail-closed **reference wrapper**, not automatic installation of a repaired producer. Concurrent modifications after preflight remain a potential TOCTOU hazard; an authorized receiver must hold exclusive ownership/lock or use a truly atomic target handling strategy. Never run on live Codex worktrees.

## Reproduce

With Python 3.12, .NET8, Git original refs fetched, and a disposable cloned checkout:

    python -m unittest discover -s Tools/B01/B01-62/P17W13SaveRollbackContract -p test_contract.py -v
    python Tools/B01/B01-62/P17W13SaveRollbackContract/make_fixtures.py <new-external-folder>
    git show d94400d95d13db7f27e1533df05c9292d5380431:Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs > Tools/B01/B01-62/P17W13SaveRollbackContract/OriginalW06/DebrisImpulseCore.cs
    dotnet run --project Tools/B01/B01-62/P17W13SaveRollbackContract/OriginalLedgerQA.csproj -c Release -- <fixture-attached> <fixture-detached> <fixture-both>
    python Tools/B01/B01-62/P17W13SaveRollbackContract/rollback_witness.py --repo . --output <new-external-receipt.json>

On CI original W06 code and snapshots are built in ephemeral runner scratch, never committed to the repository. Raw source/ledger/rollback evidence and SHA256 are uploaded per OS. Existing results **must** be checked for current exact QA HEAD; documentation of test intent alone is not PASS.

## Gates and receiver

**B01-62**: read-only original-byte source and executed reference contract; independent rollback original RED + new wrapper refusal **only after CI executes**; no claim this is a durable production save adapter. **B01-23** owns a separately versioned real loader/saver for detached stable IDs and no mutation of #510; **B01-40** owns receiver locking/staging/rollback and should integrate a properly reviewed empty-directory guard outside frozen source; **B01-61** independent snapshot+W06 transaction/rollback tests; **B01-60** genuine Unity 6000.3.14f1 licensed EditMode/PlayMode/PhysX G1 with #532; **B01-00** sole #79 handoff. Source-G0 #521 remains independent; native G1 = NOT_RUN unless original XML+Editor.log, G2 = HOLD, CODEX_ACK = NONE, NOT_INTEGRATED, NO MERGE. Revert only this isolated Draft PR and ephemeral QA scratch; never overwrite frozen producers.
