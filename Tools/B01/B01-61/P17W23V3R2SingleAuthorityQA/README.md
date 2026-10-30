# B01-61 PASS 17 WAVE 23 — SELECTED V3 #729 vs R2 #712 REAL original dual authority independent QA

Dispatch: [HQ #731](https://github.com/C8LyNn/RoboRougelike/issues/731). One selected B01-23 canonical API #681 → #691 → **V3 #729 immutable `b8ca29c7707cd6b4338d0b9772d74dee676d49d2`**. Rival #719/#695/#682 ALT_HOLD. One B01-62 session R2 **#712 immutable `bd8905ad549a7b26854b671be971f7d781308311`** and physically authoritative envelope store **#669 `d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121`**. Original frozen #510, #556, #596, #642, #647 and selected R5 #690 unchanged. This independent QA branch is based EXACT #729 head and adds ONLY five new B01-61 QA files, never changes frozen owner or public production source; no merge.

## Observed architecture issue to independently verify at source level

Selected B01-23 V3 new `CanonicalWorldSlotLease.TryAcquire(root,run,slot,...)` takes `FileShare.None` under `root/b0123_canonical_world_authority/run_{run}_slot_{slot}.owner`. Original B01-62 #712 R2 session `CanonicalSlotSessionR2.TryOpen(directory,run,slot,...)` independently acquires a separate genuine `FileShare.None` lock at `directory/slot.lease`. These are **two different OS locks**, despite one logical run+slot+original physical `save.envelope`. Both can succeed; the old R2 session can physically publish while a V3 gateway is live. Neither may be co-installed or described as one authority. This independent QA tests the real original classes together rather than pretending #691 standalone QA or #712-alone QA proves integration.

Direct immutable original code compiled:
- `Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs` from #729, Git blob `21733278b6ab0669fccb6d085e0b6cf242792de2`
- `Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs` from #729, Git blob `a9331dd620336d8c42165d134dc8a5e05ed51873`
- `Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs` original #712, Git blob `fefc2e263e068b913c6e59ec3c8375bbbe43db4e`
- `Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs` original #669 Git blob `db7745635164d9eb6b10f3246538700158130b7b`
- original frozen #556 `DetachedSnapshotContract.cs` Git blob `b52e511c370f5fca56c74029050844dc8cbb7f9a`
- W06 `DebrisImpulseCore.cs` blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`
- original #510 `SecureRigSpec.cs` blob `b680ed0ec969ac23203fa55eab7b90f8daa86e43`

The original on-disk `SingleFileSaveSlot`, true `FileStream(FileShare.None)`, atomic `File.Move`, frozen #556 binary snapshot, original W06 consumed IDs and all V3 and R2 implementation code are REAL. The private `ICanonicalRigFactory`/staged-world models are synthetic C# fault injection, NOT genuine Unity Editor/PhysX/frozen private #510 `Rig[]` + `AtomicDockLedger`. Do not interpret log label disk generation 1 as native Unity G1.

11 scoped executable C#9/.NET8 checks per OS:
1. Four original #556/W06 Light/Heavy consumed stable-ID bit combinations.
2. Within V3 only, actual original physical .owner same-run/slot second acquisition denied, `BindOnce` duplicate denied, same scope owner release after proof.
3. Wrong run rejected before staging.
4. Partial owned stage allocation then bad cleanup retains actual V3 .owner, bad physical proof fails closed.
5. False/throw physical-retirement callback keeps actual V3 .owner.
6. Original R2+single-file #669 physical g1 Light → g2 Heavy, cold correct g2 replay and same-generation consumed-ledger fork denial.
7. **Expected source release RED:** full original R2 physical g1 Light committed and ACTIVE; separate original V3 OS .owner acquired for same run+slot using R2's true durable commit port; V3 cold replays original g1 Light; original R2 g1 Light remains ACTIVE. Two live actual original owner API gateways coexist; two real OS locks physically held simultaneously.
8. **Expected source release RED with real OS CHILD CLR:** first create genuine R2 original g1 Light, physically retire original R2 parent, V3 acquires and cold replays Light while holding original OS .owner lifetime. An independent **real additional CLR process** acquires original #712 #669 .lease while V3 is still LIVE, advances original physical `save.envelope` from g1 Light to g2 Heavy with authentic frozen #556/W06 ID, waits to prove overlap. V3's own new source checkpoint preview correctly detects stale epoch and sticky quarantines, but **that is after the second writer has already committed**; no single authority. Child exit0 and original physical g2 maintained.

## Release disposition and required fix

A green GitHub Actions job means source QA successfully detected both original split-owner cases, NOT that combined product is green. Source release intentionally returns code3 when all 11 PASS and declares `HOLD_TWO_INDEPENDENT_DOT_OWNER_DOT_LEASE_AUTHORITIES`. The actual B01-23 #729 alone newly prevents same lock V3 gateways, and #712 alone prevents same .lease concurrent sessions; **the combined protocol remains unsafe until there is one physical OS lifetime authority across both worlds and storage**. B01-23/B01-62 must author a NEW separately versioned common owner/bridge which takes exactly ONE shared physical lease across gateway+store, enforces it prior to any staging/preview/save, disallows untrusted external independent R2 instantiation or bypassing .owner, and keeps lease through all root teardown/quarantine, lost ACK and cold replay. No cherry-pick/copy of ALT_HOLD #719/#695, no rewrite of frozen V3 #729/R2 #712.

Original selected R5 host #690 component-only Destroy source status separately independent #705 SOURCE_SCOPE_GREEN, but R5 authored native ColdRestart fixture expects generation4 though actual persisted gen5; B01-23 must author a NEW fixture-only version and independent retest; no native claim. Real frozen #510 private Rig[]/AtomicDockLedger Unity capture/hydration still missing. #669 valid external old-envelope and equal-generation ID fork #680 require trusted rollback model; optional #709 independently guarded highwater not owner-approved and both save+witness rollback remains possible. G0 B01-40 must formally receive EXACT NEW bridge and R5+API+storage original .meta GUIDs, manifest collision/foreign EMPTY/dirty/symlink migration, not re-use old source G0. Native G1 on user Windows Unity6000.3.14f1 NOT_RUN: operator #711 manual only, no Editor.log/NUnit XML; no PC access or Codex ACK. G2 RELEASE HOLD.

After running, record exact new PR head, actual Windows/Ubuntu original raw job logs and SHA256 JSON manifest from `receipt.py`. Status `IMPLEMENTED=YES / TESTED=PENDING_CI / QA_ACCEPTED=NO_FINAL / READY_FOR_INTEGRATOR=NO / INTEGRATED=NO / RELEASED=NO`; receiver B01-62 (single bridge), B01-23 (V3/host), B01-41 cross-process QA, B01-40 physical G0, B01-60 native, B01-00 release governance. No merge.
