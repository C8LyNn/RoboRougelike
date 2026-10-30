# B01-62 — PASS17 / WAVE17 — ONE Final Adapter #604 Physical Intake

## One exact candidate, strict authority

**ONLY SELECTED B01-23 producer**: [#604](https://github.com/C8LyNn/RoboRougelike/pull/604) **`1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a`** (`B01-23-UNITY-SAVE-HOST-BRIDGE-001 v2.0.0`). **ALTERNATIVE HOLD:** B01-23 [#596](https://github.com/C8LyNn/RoboRougelike/pull/596) `efb2b06eafedab5469b57f163152d96aca3c5915`, separately tested by B01-61 #607, not co-installed. Historical W15 #587/#585 SOURCE RELEASE RED and own B01-62 #577/#584/#597 remain references, NOT canonical adapters. Frozen only original gameplay [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) `d94400d95d13db7f27e1533df05c9292d5380431`, frozen only Save schema [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) `33094f154c7b91119b36d58aab285e6e83c5de57`, Codex and old W06 original untouched. **No merge, no source producer modifications.**

## Why #604 and security disposition

#604 contains the complete cumulative Unity source/PlayMode/demo host, corrected actual #587-lineage `AtomicWorldSwap`, new `FileCheckpointFence` and explicit `UsePersistentCheckpointSlot` opt-in, plus actual original deterministic owner physical package ZIP; 254/254 original **producer** source assertions across Win+Ubuntu [producer CI #41097514646](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097514646). B01-61 [#603](https://github.com/C8LyNn/RoboRougelike/pull/603) compiled the SAME original immutable `AtomicWorldSwap.cs` blob `399d3cb2074a2659af8f4e941edb28a3d2dd2832` and `CheckpointFence.cs` blob `53f907125b09531ebdf5ffdfbb9c5c9b48eca217` on an earlier parent #604 SHA, 52/52 genuine CLR source checks on EACH OS incl actual multi-process file fence. Its older source-only tests **do not independently certify the complete latest #604 Unity-facing host, new package ZIP or native engine**; current B01-62 work covers physical selected-current-SHA closure separately. B01-61 needs exact-final-HEAD 572/582/588 and original producer holistic native QA before release.

**Remaining security scope**: `FileCheckpointFence` is an 88-byte checksum+flush+rename watermark, **not an atomic Save+fence two-file transaction**, no guaranteed directory fsync, no verified exclusive writer lock, and SHA256 is integrity/corruption detection not authentication. Updated Rigidbody velocities/poses at unchanged durable generation cannot be saved; a real owner-controlled per-slot checkpoint save/epoch advance is needed. Persistent fence must be explicitly enabled; default demo uses volatile fence. Critically, #604's isolated new Unity world owns its own W06 consumed-ID ledger, **NOT frozen #510's private `Rig[]` / `AtomicDockLedger`**, which require B01-23/Codex-owner approved separate versioned capture/hydration/publish interface; no reflection or canonical game installation. Source confidence does NOT equal native `G1`.

## What this independent QA physically executes

`intake.py` checks out exact selected #604 as an **entirely disposable detached Git worktree**, loads and ACTUALLY EXECUTES the **unchanged original B01-23 `package.py`** (Git blob `96dc5475505172a63ed2510098eb6fc8a234ee18`) to create and verify the original inner ZIP. Expected **101,354 bytes / 27 original members**, SHA256 **`ef110b5906d385cbc51938a35510c97516a0942b28ec055519a78d1e3fe5afb8`**. Independently compares ALL 26 owner source files in that physical ZIP to actual original `git show #604:path` byte-for-byte and original manifest SHA256/size; requires original source/parent SHA pins, `codex_ack=false`, `native_unity=NOT_RUN`, `canonical_510_live_import=HOLD`. Revalidates source C# blobs: `AtomicWorldSwap.cs` `399d3cb2074a2659af8f4e941edb28a3d2dd2832`, `CheckpointFence.cs` `53f907125b09531ebdf5ffdfbb9c5c9b48eca217`, `IsolatedTwinRigSaveHost.cs` `5012e98bba786c6b9702992c317586a0015678c1`. Five immutable #510/#556 original game/blob pins enforced.

**NEW INDEPENDENT TRIPLE-ASSET COEXISTENCE CHECK**: reads original exact selected physical 11 Unity `.meta` GUIDs + exact original frozen #510's 14 Unity GUIDs + original frozen #556's 9 Unity GUIDs, rejects any duplicates across **34 distinct package GUIDs** or overlapping asset paths, validates original three asmdef names and ensures no serialized legacy W07 or alternate #585/#596 assets are bundled. No canonical Codex file collision or actual Unity import is claimed.

Executes **actual unchanged original #604 `stage()` / `rollback()`** on a newly created external scratch path, requiring byte-identical stage and exact ownership rollback. Negative cases: unrelated root/nested EMPTY directory, modified original file, alien user file, symlink (if OS allows; skipped with honest `NOT_RUN`), missing original file, pre-existing destination, forged release metadata `native_unity=PASS`/`codex_ack=true`, and attempt to stage into original Git checkout. User/foreign data and private Git worktrees cannot be overwritten or deleted. Does NOT run original #510/#532 destructive rollback.

**No second installer or duplicate Save bridge:** this is a physical receiver-specific provenance / cross-package co-install QA gate; formal B01-40 G0 must independently accept exact #604 source ZIP and safe [#536](https://github.com/C8LyNn/RoboRougelike/pull/536)/[#575](https://github.com/C8LyNn/RoboRougelike/pull/575) migration. B01-60 #542 applies only to its native QA receiver. Actual shared/legacy Codex scene migration is unapproved.

## Reproduction

With Git, Python3.12, selected #604 + frozen SHAs fetched in a disposable checkout:

    python Tools/B01/B01-62/P17W17Selected604PhysicalIntake/intake.py --repo . --output <new-external-receipt.json>

The dedicated two-OS CI outputs actual source/physical-check counters and stable receipt SHA256, uploads reproducible original stdout+JSON artifacts; inspect exact SHA before claiming PASS. Native Unity 6000.3.14f1 EditMode/PlayMode/PhysX test suite #532/#567/#581/#601 requires real activated Editor + XML/Editor.log, else **G1_NOT_RUN**.

## Gates, owner handoff and rollback

`SELECTED_#604_SOURCE_ONLY / #596_ALT_HOLD / B01-62_PHYSICAL_INTAKE_PENDING_UNTIL_CI / #510_#556_FROZEN_ACCEPTED_SOURCE_ONLY / B01-40_FORMAL_#604_G0_PENDING / LIVE_CANONICAL_SAVELOAD_HOLD / UNITY_G1_NOT_RUN / G2_HOLD / CODEX_ACK_NONE / NOT_INTEGRATED / NO_MERGE`.

**B01-23** owns correction/versioning and NEW authorized canonical #510 private-rig/ledger API; **B01-61** independent selected final SHA transaction/ID/rollback; **B01-40** formal physical G0, source hash, GUID, safe exclusive migration; **B01-60** licensed Unity native G1; **B01-00** sole #79 handoff / Codex ACK coordination. Rollback of this own QA is to close this Draft/remove only new B01-62-owned files. Original #604 and frozen producer branches never touched; any scratch worktree is removed after verification. Source signature of missing live host API and native G1 remain blocking even when physical intake passes.
