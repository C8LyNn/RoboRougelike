# B01-62 W29 — Cold-Read V3 (NO new Save writer)

**Goal:** old #826 has a correct one-file writer but the real Unity loader needs a safe way to retrieve the authoritative frozen #556 **world bytes + binary ECON V7** before calling the typed #826 gateway Restore. This additive helper opens the exact same physical slot under a short-lived original #729 OS-exclusive \`.owner\`, invokes original #826 \`WholeGameEconomySlotV3.Open/Read\` unchanged, retires the genuine Gateway and proves the lock is released **before** returning a copy of the world+wallet. It creates no GameObjects, does not run physics, never touches a second writer, and refuses when another owner is live, the envelope is corrupt, or retirement cannot be proven.

New class: \`B01.CanonicalColdRead003.CanonicalColdReadV3.ReadClosedSlot(root,run,slot,out gen,out world,out economy)\`. Intended **cold boot** call order is: read while there is no live session → verify no error → acquire the normal canonical #826 session with genuine #792 main-thread physical factory/gate → call existing \`TryRestore(world,economy,run,generation)\`. In that final step the original store and gateway reject any CAS generation or economic fork that arrived between cold peek and actual reopen. The caller must retain the session and ensure all staged Unity roots are proven destroyed before releasing the OS owner.

**Not a drop-in GUI integration:** original game R3 currently lacks a production typed #792 ↔ V7 binder and frozen #510 has private Rig[]. Installation is forbidden until independent composite G0 and B01-00 selection. Do not copy an entire historic producer branch or raw #787 store. Do not infer free-victory/loaner legitimacy from read-only files; authentic receipts belong to combat owner.

## Tests and scope
- Windows/Ubuntu source CI compiles original #826 *unchanged* along with original #780 exact V7 two-file source and #729/#556 real code, C#9 \`netstandard2.1\` and net8.
- 5 original-OS tests: empty slot no lease creation; g1 cold read + V7; g2 actual atomic replace + fixture distinct victory/cold read; live writer Busy with **zero** returned bytes; corrupt full envelope fails closed with owner retired.
- Real Unity 6000.3.14f1 native EditMode/PlayMode/Player **NOT_RUN**. Source tests are not in-game outcomes. No power-loss fsync proof.
- Formal independent B01-40/41/61 current composite G0 pending; B01-23/B01-20 single native binder pending; B01-60 native G1 pending. This branch is **producer-side additive read code only**, not selected/installed game.
