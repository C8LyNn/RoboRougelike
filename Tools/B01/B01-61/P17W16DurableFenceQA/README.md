# B01-61 P17 WAVE16 — independent ORIGINAL W16 durable fence acceptance

**New exact B01-23 W16 producer source** branch `b01-23/p17w16-one-atomic-save-host-r2` at SHA `7d206c63e1e9ab4ee4ce52eb314507ce49721d2c`. Direct original `AtomicWorldSwap.cs` Git blob `399d3cb2074a2659af8f4e941edb28a3d2dd2832`, newly authored original `CheckpointFence.cs` Git blob `53f907125b09531ebdf5ffdfbb9c5c9b48eca217`. **Only selected Save #556** `33094f154c7b91119b36d58aab285e6e83c5de57`, **only frozen game #510** `d94400d95d13db7f27e1533df05c9292d5380431`, original W06. No #585, #577/#584 alternative integration, no canonical source edits.

**What is new vs W15 #588 and W16 #592:** directly compile *new* B01-23 W16 checkpoint/authority code with original selected #556 codec and W06 ledger. Real filesystem watermark created, verified, advanced, corrupted and read by **SIX DISTINCT .NET CLR PROCESSES per OS**; NOT a cloned or simulated persistence class. Initial source host transaction (additional process) fault-injects post-activation world corruption and failed rollback; test code substitutes only `IWorldFactory/IPreparedWorld` physical implementation. No fake editor/PhysX claimed.

## Release gating on actual W16 source

- W15 [#588](https://github.com/C8LyNn/RoboRougelike/pull/588) historical equal-generation same-run different-world RED -> W16 SOURCE GREEN if original `TryLoad` now refuses **before** preparing any world, with Light ID and actual W06 consumed state preserved.
- W15 [#582](https://github.com/C8LyNn/RoboRougelike/pull/582) first-call false normal reject after failed cleanup -> W16 SOURCE GREEN if **first original core call** returns `RollbackQuarantine`, and subsequent Load/Save denied with sticky quarantine.
- W15 [#572](https://github.com/C8LyNn/RoboRougelike/pull/572) staged candidate changes identity only in `Activate` -> source must re-Capture **after** activation, reject and compensate to original old world/ID.
- Durable fence real OS process #1: original `FileCheckpointFence.Seal` creates exactly 88-byte SHA256 watermark to private scratch path when accepting original Light-detached/Heavy-attached selected #556 checkpoint gen31.
- Separate OS process #2: true cold process with new original W16 host and same *exact byte-identical* gen31 checkpoint reads watermark; consumed Light ID/Heavy attached reconstituted. Original `TrySave` allowed with **identical hash**, but physically changed detached position **with same generation** is denied by durable Check.
- Separate OS process #3: alternate valid detached Heavy/attached Light checkpoint **same run+gen31** rejected BEFORE world staging by persisted hash. Also old gen30 rejected. No false quarantine on clearly stale/foreign.
- Separate OS process #4: strictly newer gen32 checkpoint accepted and original watermark advanced via actual flush+replace, now original consumed Heavy ID.
- Separate OS process #5: older byte-identical gen31 checkpoint cannot be replayed on independent process after committed gen32, no world mutation.
- Separate OS process #6: controlled one-byte corruption of ONLY disposable 88byte checkpoint marker -> original `FileCheckpointFence.Check` `Unverifiable`, core MUST quarantine without candidate creation, no release or physical state published.

**Honesty:** `FileCheckpointFence` is an actual durable fence but **NOT complete authoritative crash-safe Save+fence atomic file transaction**, no HMAC against malicious input, no two-file crash recovery or external file lease/locking tested. The B01-23 Unity-facing implementation remains an **isolated twin-rig demo** with its own W06 ledger; it is NOT canonical private #510 Rig[]/AtomicDockLedger save/hydration. Native licensed Unity6000.3.14f1 Edit/Play/PhysX **NOT_RUN** in this QA; B01-60 owns real Editor XML/Editor.log. No Codex ACK, no merge, G2 HOLD. Do not conflate *source acceptance of the new checkpoint fence* with full game Save/Load.

**Owner and intake:** B01-23/62 must freeze ONE exact bridge version and certify production authority of real checkpoint file + original #510 private rig capture; B01-40/62 exact actual W16 inner ZIP/GUID/asmdef and safe #536/#542 receiver rollback; B01-60 licensed Unity G1 or NOT_RUN; B01-00 controls Codex #79 handoff. This unmerged QA PR adds only six independent QA-only files; rollback closes Draft. [HQ #589](https://github.com/C8LyNn/RoboRougelike/issues/589).

```bash
python Tools/B01/B01-61/P17W16DurableFenceQA/source_guard.py
dotnet build Tools/B01/B01-61/P17W16DurableFenceQA/OriginalW16FenceQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W16DurableFenceQA/OriginalW16FenceQA.csproj
# Then run seal, restart, fork, advance, stale, corrupt with same scratch watermark path
```
