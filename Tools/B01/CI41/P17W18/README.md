# CI41-596-CROSSPROCESS-CAS-001/R1 — PASS17/WAVE18
Owner: B01-41 CI/CD & TEST AUTOMATION. HQ: https://github.com/C8LyNn/RoboRougelike/issues/632

## New executable security and concurrency test — not copied code
This is independently authored **REAL multi-process same-checkpoint race testing** of the owner-selected and immutable #596 exact SHA `efb2b06eafedab5469b57f163152d96aca3c5915`. #604 is ALT_HOLD, NEVER installed. This PR is 7 new CI41 test files ONLY on the frozen #596 stacked branch. Original frozen gameplay #510 `d94400d95d13db7f27e1533df05c9292d5380431`, sole Save schema #556 `33094f154c7b91119b36d58aab285e6e83c5de57` and original B01-23 producer source are unmodified.

`Original596ParallelCheckpointQA.csproj` directly compiles **ORIGINAL #596 `AtomicWorldSwap.cs` with its own real `FileCheckpointStore`**, ORIGINAL selected #556 `DetachedSnapshotContract.cs`, ORIGINAL W06 `DebrisImpulseCore.cs`, and frozen #510 `AtomicDockLedger.cs`. No mocked checkpoint authority, no copied C# Save implementation.

`Program.cs` runs **12 real simultaneous external CLR process pairs (24 child processes)**, synchronizing each pair at a physical filesystem START flag to race `FileCheckpointStore.Commit` on a newly created physical checkpoint in one shared per-race directory. Covers (four races each): same run/different full 64-digit checkpoint digest, same run/identical digest, *different* run identities. Every race requires exactly ONE winning process, exact winning run/epoch/hash readable from persistent original sidecar, loser must not rearm generation1, foreign run must be unreadable, legitimate next-generation advance must persist, and no stale temp `*.tmp` files. Additional tests hold actual OS `.lock` while original Read and Commit must deny without mutations, then unlock and recover, and corrupt original on-disk sidecar must fail closed without overwrite. Own process outputs machine-parseable `CI41_W18_REAL_ORIGINAL_596_CROSS_PROCESS_CAS_PASS`.

Each race uses new UUID-based scratch OS temp directory under the test's own root. All subprocesses exit before deleting ONLY the unique owned test directory. NEVER read or delete `Application.persistentDataPath`, user saves, other agents' worktrees or foreign directories. No merge; release remains HOLD.

**Distinct from previous tests:** W17 CI41 #631 17 original-source assertions demonstrated SEQUENTIAL native test fixture contamination and one shared file path across original PlayMode run IDs 31/41/9. B01-61 #607 verified sequential FileCheckpointStore compare-and-swap; B01-61 #629 checked Unity-facing fake host and sequential process restarts. W18 checks **truly parallel physical separate CLR processes** contending for the one original file, under 3 ownership/digest scenarios and during held actual OS filesystem locks, which were not certified by those runs.

## CI and gate boundary
Dedicated GitHub Actions runs on Ubuntu24.04 and Windows2025 and executes strict `dotnet build --warnaserror`, original-source 24 child process race suite, and 18 adversarial independent Python tests of exact Git blob/owner/scope/proof authenticity. `source_guard.py` reads exact original #510/#556/#596 commit objects, tests **all seven W18 files ADDED only**, pins five original producer/codec/W06 Git blob IDs, rejects change of original files or forged output. Machine JSON and original stdout are retained as artifacts.

A green result **ONLY attests source-level cross-process CAS locking on one original checkpoint file**. It does not prove safe **multiple simultaneously active Unity host instances**, global lease, snapshot+checkpoint two-resource crash-atomic power-loss durability, native Unity Rigidbody, original #510 private Rig[]/AtomicDockLedger capture/rehydration, or real game Save/Load. Original W17 defect (global `Application.persistentDataPath` hardcoded native test file across multiple PlayMode fixtures) remains separate `NATIVE_TEST_ISOLATION_HOLD` until owner versioned fix.

Receivers: B01-23/62 for new versioned exclusive host slot authority and per-test fixture path, B01-61 for independent new selected SHA full transaction/rollback retest, B01-40/62 physical #596 G0 and safe disposable migration, B01-60 for **actual licensed Unity6000.3.14f1** NUnit EditMode/PlayMode/PhysX with real XML and Editor.log (otherwise `NOT_RUN`), B01-00 sole Codex authorized source-only handoff and actual ACK.

Lifecycle:
`IMPLEMENTED=YES; TESTED=CI_PENDING; QA_ACCEPTED=NO; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO`.
`G0` for this new test artifact independent receiver pending; original #510/#556 G0 source-only accepted; #596 physical G0 belongs to B01-40. `G1_NOT_RUN / LIVE_CANONICAL_SAVELOAD_HOLD / G2_HOLD / CODEX_ACK_NONE / NO_MERGE`.
Rollback: leave own W18 Draft unmerged or discard ONLY these seven new paths. The test removes ONLY its own GUID-named temp scratch directory.

Reproduce on checkout of own additive W18 branch:
```bash
python3 -m unittest discover -s Tools/B01/CI41/P17W18 -p test_source_guard.py -v
python3 Tools/B01/CI41/P17W18/source_guard.py --repo . --output receipt.json
dotnet build Tools/B01/CI41/P17W18/Original596ParallelCheckpointQA.csproj -c Release --warnaserror
dotnet run -c Release --no-build --project Tools/B01/CI41/P17W18/Original596ParallelCheckpointQA.csproj
```
