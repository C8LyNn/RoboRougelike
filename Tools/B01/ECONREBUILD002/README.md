# B01-22 P17/W20 — ECONREBUILD002 owner-global authority (new v2 source only)

**Parent immutable:** original ECON [#252](https://github.com/C8LyNn/RoboRougelike/pull/252) @ `12572117a3dce369257feabc57021cde58023369`. Its `RebuildQueueService.cs` Git blob remains `cac95b28647528a7a46cb818c6d78037dc3e80a2`. W19 defect witness [#620](https://github.com/C8LyNn/RoboRougelike/pull/620) demonstrated TWO independent instances granting two free repairs on ONE surrogate R3 owner and accepting the same win ID; stale-save replay also reproduced. This version is a **new** public v2 API, not a patch to frozen v1.

## Concrete implementation
- `RebuildEconomyAuthorityV2` has **no local quota, wallet, victory or queue authority**. All state is fetched from `IAtomicRebuildOwnerStoreV2`, with compare-and-swap by owner/run/generation; genuine conflict causes re-read/reprice. One owner has one global free reservation in each victory epoch, even with multiple service instances.
- First epoch currently gets one bootstrap free slot, mirroring original v1; subsequent epochs unlock ONE only on a unique authenticated win. **This is a PLAYTEST HYPOTHESIS, not owner-approved balance.** Every second 0HP reservation costs authoritative quoted amount, strictly between 0 and purchase. Never repairs before next genuine win.
- The store must atomically publish canonical physical part IDs+HP+reservations, wallet, event IDs, free quota and generation to one authoritative durable slot. No private R3 fields/reflection. `Uncertain` or thrown Commit after durable write quarantines that service; never resumes stale old-state write. A host that cheats about `RejectedNoWrite` is invalid. `TryAuthorizeResume` only compares a candidate against authoritative current state; NEVER imports arbitrary old JSON or bypasses the host.
- `OwnerRebuildStateV2` is an **offstage transaction projection**, not an extra live R3 inventory. To integrate, build an authorized host adapter that stages and publishes existing R3 objects with real main-thread/physics synchronization. The test store uses real disk JSON, a cross-process exclusive lease and one-file atomic rename, but is explicitly a **SURROGATE**: not Unity, not verified power-loss directory fsync, not atomic with actual #510 world/#556 schema, and not a production Save/Load receiver.

Booked rebuild tickets retain their original paid quote even if an authorized subsequent owner CAS updates the module's purchase/quote price. A later market price change MUST NOT retroactively reprice or invalidate queued repairs; the test suite includes this scenario.

## Executable verification
```bash
dotnet build Tools/B01/ECONREBUILD002/RebuildV2.Tests.csproj -c Release
dotnet run --project Tools/B01/ECONREBUILD002/RebuildV2.Tests.csproj -c Release -- /tmp/b01-22-owner.json --scenario
dotnet run --project Tools/B01/ECONREBUILD002/RebuildV2.Tests.csproj -c Release -- /tmp/b01-22-owner.json --verify
```
Cross-process mode reads the **same actual persisted OS file**. Cases: two services claim same owner, exactly one free, second paid, true 0HP/pending, win batch preserves GUIDs, replay rejection, stale/fork restore refusal, foreign owner/run, negative no-win, unknown-ACK quarantine after a genuine file write, known pre-write reject, cold process restart. C#9 compiles the ORIGINAL V2 Unity source with .NET8; this is NOT native Unity G1.

## Balance model — not participant evidence
Deterministic toy model: 12 wins, 2 destroyed modules per victory epoch, quote 70 Scrap, purchase 100. Per-win: 12 free / 12 paid => 840 Scrap. Per-run: 1 free / 23 paid => 1,610 Scrap. **Difference: 770 Scrap**, entirely assumption-driven. Real balance decision needs B01-10 / B01-13 and human playtests, including loss cadence and impact on anti-softlock #154.

## Governance and handoff
- Only `ECONREBUILD002`, own Tools and new CI files added; no frozen #252/#510/#556/#642, canonical main/develop, ALT_HOLD, or existing receiver modified.
- B01-10: approve cadence and initial free bootstrap; B01-13: economy interaction and anti-softlock; B01-62: durable single-slot authority generation/owner/run protocol review; B01-61/41: independent exact-SHA original-source tests, including actual host boundary and lease, loss of ACK, stale replay; B01-40: independent physical/GUID/asmdef package G0. B01-60: real licensed Unity 6000.3.14f1 G1, else NOT_RUN.
- A future authorized integrator must link the canonical #510 world and #556 payload WITHOUT introducing a second physical owner. Native R3 wallet, entity identity, module removal/usage block while reserved, full crash-consistency, live G2 and player tuning are **HOLD**.
- Safe rollback: never uninstall/migrate a live save with active `Pending` or mismatched generation. Disposable isolated receiver, SHA/manifest, compare current slot, quarantine on mismatch. No merge.
