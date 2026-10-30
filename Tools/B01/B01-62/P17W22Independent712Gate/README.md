# B01-62 / PASS17 WAVE22 — independent security regression #697 RED -> #712 R2 GREEN

## Exact original source identity; one selected lineage, no mixed implementations

This standalone, **new B01-62 QA-only** package is based on immutable original owner [#712 @ bd8905ad549a7b26854b671be971f7d781308311](https://github.com/C8LyNn/RoboRougelike/pull/712), the new VERSIONED replacement for original [#697 @ ab62b041e50f8aa0defada2aadc0987d6c63f9d9](https://github.com/C8LyNn/RoboRougelike/pull/697). Both direct original adapter implementations are compiled and tested in TWO SEPARATE projects; nothing from old #697 is co-installed into R2. The single owner-selected canonical API remains #681 -> same-lineage [#691 V2 @ 809f82575c638e1b4d2806b5f417bddab6c0871a](https://github.com/C8LyNn/RoboRougelike/pull/691), competitor #682 ALT_HOLD. The one physical durable Save store is unchanged [#669 @ d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121](https://github.com/C8LyNn/RoboRougelike/pull/669). Original owner R5 host [#690 @ cc7f73541517a18f4479bb155534bd4832d9985e](https://github.com/C8LyNn/RoboRougelike/pull/690) has separate physical source G0 intake by B01-40 #699 and public managed MonoBehaviour tests B01-61 #705; R5 #693 ALT_HOLD. Original frozen #510, #556, #596, #642, #647 not changed or merged.

**Seven exact Git C# original source blobs** guarded by workflow before compiling, never a copied/rewritten producer:
- historical original #697 CanonicalSlotCommitAdapter.cs: bcecb87c8b1b198e8c086d30f26846bf202ea1c4
- corrected original #712 CanonicalSlotSessionR2.cs: fefc2e263e068b913c6e59ec3c8375bbbe43db4e
- original #691 CanonicalWorldGateway.cs: 704e774dedd5be5af26e0791fba87ecdb109ae2a
- original #669 SingleFileSaveSlot.cs: db7745635164d9eb6b10f3246538700158130b7b
- original frozen #556 DetachedSnapshotContract.cs: b52e511c370f5fca56c74029050844dc8cbb7f9a
- original W06 DebrisImpulseCore.cs: fa1aa5e531927cbe73cf5fdfe161b4a981316db5
- original frozen #510 SecureRigSpec.cs: b680ed0ec969ac23203fa55eab7b90f8daa86e43

## Actual tests: same independent fixture, original RED and exact corrected GREEN

Each OS (real GitHub Actions Windows2025 and Ubuntu24.04) STRICT .NET8/C#9 with TreatWarningsAsErrors, two independent executables: old #697 original and corrected #712 R2. Ten original-source assertions in EACH executable (40 per two-OS workflow):
1–2. Actual unchanged #556 frozen Light stable consumed ID 2310001 and Heavy 2310002/W06 ledger data.
3. Legitimate actual original #669 FileShare.None physical Save g1 Light, second independent CLR process denial until world physically retired, release, and original cold owner #556 g1 decode.
4. Source-root partial preparation fails, CleanupUnpublishedAndVerifyDestroyed returns FALSE. Actual original #691 correctly quarantines but HasWorld=false; original #697 mistakenly Dispose unlocks slot (expected frozen RED witness), NEW R2 preserves OS lease (expected GREEN).
5. Same failure when cleanup THROWS; real child process cannot acquire NEW R2 until proof of actual source-fixture root retirement.
6. External real independent CLR process attempts second owner under still-active partial-root orphan; original V1 RED, new R2 GREEN.
7. When original partial root cleanup is GENUINELY complete (managed fixture truth), healthy non-quarantined session may release and new child owns slot.
8. Corrupted SHA original #556 or wrong RunId is rejected before any partial world creation.
9. Wrong run/foreign slot GUID denied after original physical owner is explicitly retired.
10. Original same generation but alternate valid Light/Heavy world rejected; g2 must carry original durable full Save ID/hash.

The independent fixture's simulated World is a **managed stub for private #510**. Its success does not prove activated Unity GameObjects/Rigidbody PhysX, main-thread input/AI gate, or actual game Save/Load. Real original #691 source, #669 FileStream and actual physical original binary #556 are NOT stubs. Two distinct independent CLR processes attempt acquisition via actual OS handle, not a mocked lock. The source gate explicitly prints **V1 697 EXPECTED PRODUCT SECURITY RED** while its 10/10 detector cases PASS; those are NOT claims that the old product was safe. Corrected #712 must print an independent source GREEN from the same managed test scenarios.

## Separate G0/G1 decision and security limits

- Physical R5 [B01-40 #699 exact original 129088 B/27-entry Git ZIP/34 GUID](https://github.com/C8LyNn/RoboRougelike/pull/699) **G0_ACCEPTED_SOURCE_ONLY** for exact #690, independent B01-61 [#705](https://github.com/C8LyNn/RoboRougelike/pull/705) source-test 50/50 PASS; formal integrated gameplay R5 G0 still HOLD pending native and one-owner receiver acceptance. PRIOR G0 does NOT cover alternative #693, or Unity installation of #712 (this package adds zero Assets).
- Canonical API V2 #691 original source Git package [B01-40 #704](https://github.com/C8LyNn/RoboRougelike/pull/704) source transport G0 accepted SOURCE_ONLY, but old naked V2 independently [#710](https://github.com/C8LyNn/RoboRougelike/pull/710) permits two concurrently active private-world gateways without full-life slot lease. This **R2 source patch fences only callers actually using the session**, NOT every naked V2 external caller. Independent B01-61/#41 must re-test actual final consumer.
- Source G0 of new R2 source-only package is **NOT a signed Unity physical intake**. No changes to original Unity serialized references, GUID or existing owner scripts. Safe rollback is close the unmerged QA PR and ignore its newly-added own tests/CI only. No live player/Unity data migration or foreign folder cleanup.
- Valid externally copied past envelope g1 and same-gen different-ledger still bypass a **self-authenticating** original #669 file under explicit hostile disk replacement threat, proven original [#680](https://github.com/C8LyNn/RoboRougelike/pull/680). Optional external trusted-root authority [#709](https://github.com/C8LyNn/RoboRougelike/pull/709) can reject rollback IF the trusted root itself cannot be rolled back by the adversary, but restoring BOTH valid Save and receipt still defeats this model. **Do not automatically mix #709 with #712**; owner policy, independent cross-file crash consistency and a separate authorization are required. No claim real hardware power-loss parent directory fsync.
- Actual locally licensed Unity Editor 6000.3.14f1 + PhysX EditMode/PlayMode **G1_NOT_RUN**; [B01-60 operator #711](https://github.com/C8LyNn/RoboRougelike/pull/711) preflight pass is not Unity activation. Real original frozen #510 private Rig[] and AtomicDockLedger capture/hydrate absent, LIVE_GAME_SAVELOAD_HOLD, G2_HOLD, CODEX_ACK_NONE, NO MERGE.

**Stages:** IMPLEMENTED=YES (new independent QA); TESTED=SOURCE CI check on final own SHA; QA_ACCEPTED=TESTED_SCOPE_ONLY after independent run; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO. B01-40 is separate G0 physical owner, B01-61/41 owns full security regression, B01-23/20 canonical private Unity implementation, B01-60 manual Windows Unity native; B01-00 future authorized Codex integration. Actual integrator feedback NOT_MEASURED.

## Package scope and rollback

The package contributes only new B01-62 Program.cs, two strict .csproj, README.md, manifest.json and dedicated B01-62 workflow. It is branched on the exact original #712 parent; all prior B01-62 #712 implementation, selected #691/#669 and frozen original sources remain unchanged. Unmerged QA rollback by closing Draft; no actual active player Save or owner-controlled physical installation touched.
