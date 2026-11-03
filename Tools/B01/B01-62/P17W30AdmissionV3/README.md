# B01-62 W30 — secure same-writer V3 ECON #855 admission

**Exclusive successor of #858 and #826, NOT a second Save writer.** Patches the two *existing* B01-62-owned V3 production source files in this isolated branch. Same on-disk `B0162G02`, original `slot.canonical-game-v2` marker and original #729 single OS owner, `File.Replace` with flush/readback, immutable frozen #556 world bytes, original #780 V7 binary section, all part GUIDs and generations unchanged.

Imports the **byte-exact genuine B01-22 #855** `EconomyV7AdmissionW29`, not a policy clone. Store.SEMANTIC and Encode/Decode enforce original full owner/Run/Generation from the canonical session. Session.ValidatePair rejects invalid V7 *before* any physical stage. This closes the source **original V7 WIN0 + pending FREE** bug for the selected Save, including importing a checksum-correct old malformed envelope. It quarantines rather than auto-repairing/migrating bad existing Saves. Legitimate zero Scrap PAID (purchase P1) and a real first win's FREE quota remain valid. No anti-tamper guarantee and no changes to #834 owner-policy or borrower softlock.

### Executable evidence and scope

New CI compiles original V3 source + #855 + exact original #780 under C#9 netstandard2.1; executes retained 9 W28 owner tests and 6 W29 cold-read tests, plus 4 new OS-disk V7 admission tests on Windows2025/Ubuntu24.04 (target 19/OS). Previous W28/W29 source guards still run on their *original branches*, not this changed descendant where their fixed 8/9-file scope would incorrectly reject the new 13-path versioned delta. Only original B01-62-owned production sources are modified in successor; unchanged original producer, game and frozen source.

**HOLD:** not yet accepted as an independent exact-SHA G0 composite with original #792 Rig/ledger, #750 R5, #855 GUID/asmdef and active game #873/#877; no native Unity 6000.3.14f1 EditMode/PlayMode/Windows Player, no real combat receipt or runtime in-game Save/Load. B01-40/41/61 re-run composite for exact W30 SHA, B01-23/20/Codex connect one game host after owner selection, B01-60 executes native Unity tests. No protected merge or release.
