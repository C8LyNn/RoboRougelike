# B01-00 / PASS17 WAVE16 — exact code-body-aware bridge HOLD

## Selection and original-source provenance checked using GitHub

- **Immutable gameplay** [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) SHA `d94400d95d13db7f27e1533df05c9292d5380431` and **immutable Save schema** [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) SHA `33094f154c7b91119b36d58aab285e6e83c5de57`, both untouched.
- **Source candidate A [#585](https://github.com/C8LyNn/RoboRougelike/pull/585) EXACT PR HEAD `135a67d42acfccd59ca9145e09630d0b4bbade1c`**: original C# core `AtomicColdUnityHostBridge.cs` Git blob `e9e9dc6d437e96dad6b08c1639e8bd94d62c4f4b`; [B01-61 independent #572](https://github.com/C8LyNn/RoboRougelike/pull/572) tested EXACT original SHA/blob and reproduced source-level **`ACTIVATION_POSTVALIDATION_BYPASS`**. An `IIsolatedRigWorld.SetVisible(true)` callback may mutate ID/world state after pre-activation validation; bridge returns Accepted without post-activation equality check. **This is current-exact-source SECURITY_RED, G0 HOLD**, notwithstanding source producer test PASS. It was not a native Unity/PhysX experiment.
- **Source candidate B [#587](https://github.com/C8LyNn/RoboRougelike/pull/587) current PR HEAD `ad7904eca571c742257551fa8103f266fcb9b752`**: current `AtomicWorldSwap.cs` Git blob `16a7d7043a2ecb59a41bbfb063170235872fd7fc` (verified from GitHub physical original source). [B01-61 #588](https://github.com/C8LyNn/RoboRougelike/pull/588) actually executed *earlier original* producer commit `39f03fd6a0d5fae10be5419db4beb79201147bc8` / core blob `db69bae2bf4cf106da68f13e0b067499f0da02f3` and RED-reproduced **same-run/same-generation different-world fork**, but that is NOT exact current HEAD. The new #587 core was modified (it now performs post-activation `Capture/Consistent`), yet its generation-check still appears to use less-than rather than strict same-generation content/revision authority. **Result: `HOLD_CHANGED_CORE_NEW_EXACT_SHA_QA_REQUIRED`**, not false exact current source RED nor accepted PASS. Independently test it on the new SHA before formal receipt.
- **Unselected B01-62 [#577](https://github.com/C8LyNn/RoboRougelike/pull/577) QA [#582](https://github.com/C8LyNn/RoboRougelike/pull/582)** actually found a same-generation world fork **AND** a swallowed cleanup `Abort()` exception that sets quarantine but returns an ordinary Rejected verdict; this is a *different producer core* and must not be mislabelled as #587's QA. #577 and #584 are alternative ports, not third authoritative adapter.
- Existing #510 source-only [Codex Inbox comment](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6526629723) is NOT Codex ACK/CLAIM. No new competing source #585 or #587 offered or merged by B01-00. The issue #589 suggests choosing #587 as a *starting point* but explicitly reserves final exclusive source choice to B01-23 and B01-62, not coordinator.

## Original B01-00 functional repair and actual test contract

Extend existing original `save_admission.py` with **`--bridge-w16`**, not a competing Unity host/Save codec. `bridge_w16_hold.json` pins original physical PR HEAD and original Git blob SHA for both candidates, exact older #588 test target, separately scoped #582/#577, and immutable parent Save/gameplay sources. The offline executable prevents a producer source CI green from silently cancelling independent security RED, refuses to transfer older QA evidence to a changed core without source-blobs matching, requires exclusive B01-23 choice with an explicit canonical decision receipt, and forbids invented G0, native G1, physical live #510 SaveLoad, canonical migration, unsafe rollback, Codex ACK or G2.

Run:
```sh
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/save_admission.py --bridge-w16 Tools/B01/B01-00/W09/RECEIPT-LATCH-001/bridge_w16_hold.json
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p "test_*.py" -v
```

New `test_bridge_w16.py`: **57 positive/negative executable original producer cases** (prior original W09–W15 287 → W16 total **344**). It tests real SHA/blob current vs prior, selection authority, exact original QA targets, rejecting #577/#584 blending, intentionally distinct #582 port faults, fake Unity/G0/ACK/merge, damaged frozen-source identity, source-holder overwrite and strict CLI exit 2 when real acceptance is requested. This tool itself uses NO GitHub network or Unity; GitHub values above were checked externally through authenticated connector. A self-consistent input is not an authenticated GitHub user or an independent QA release.

## Gate and rollback

Expected actual receipt before B01-23 creates a corrected new owner-controlled SHA:
```json
{
 "owner_decision_state":"UNDECIDED",
 "selected_source":null,
 "source_statuses":{
   "585":"HOLD_CURRENT_EXACT_SOURCE_SECURITY_RED",
   "587":"HOLD_CHANGED_CORE_NEW_EXACT_SHA_QA_REQUIRED"
 },
 "independent_exact_sha_qa_current_green":false,
 "source_g0":"HOLD",
 "native_unity_g1":"NOT_RUN",
 "live_game_save_load":"HOLD",
 "g2":"HOLD",
 "codex_ack":"UNCONFIRMED",
 "integrated":false
}
```
**Own scope:** existing B01-00 `Tools/B01/B01-00/W09/RECEIPT-LATCH-001/**` + its dedicated workflow only. Zero `Assets/**`, original B01-23/62 bridge assets, frozen game/Save, W06, Codex-owned scenes, QA owner paths, main/develop or merge. On rejection or rollback discard only unmerged B01-00 disposable tool/code/output, NEVER original frozen #510/#556 sources or user/foreign folders. Approved receiver rollback remains disposable B01-40 #536/B01-60 #542 fail-closed; **do not use original #510 recursive rollback**.

**Receivers:** B01-23/62 choose exactly one NEW corrected source version and fix original #572/#588/#582 findings in their OWN files; B01-61 independently re-execute exact original selected SHA (not older test SHA); B01-40/62 formal physical receipt and safe migration, B01-60 activated native Unity or genuine NOT_RUN, B01-00 claim inventory. **G2/HOST_SAVE/HUMAN CODEX ACK all HOLD.**

## W16 post-snapshot OWNER DECISION / NEW EXACT ORIGINAL C# QA — supersedes preceding initial queue state

B01-23 **selected only the #587 lineage** and published **new unmerged Draft [#596](https://github.com/C8LyNn/RoboRougelike/pull/596)** for new R2 v2.0.0 repairs. Original #587 SHA `ad7904eca571c742257551fa8103f266fcb9b752` and #510/#556 frozen parents remain immutable. #585, #577, #584 alternate HOLD. The #596 code is **NOT independently accepted**; its observed *mutable Draft* HEAD when this source fixture was written was `7bd598fde2204d6a397a50b199be12f072b5cd66`. A newer owner commit must be verified anew; this is not a frozen release identity.

Independent B01-61 [**#590 at QA HEAD `c28565639bef834e64c092ce142c449acdfb7ae5`**](https://github.com/C8LyNn/RoboRougelike/pull/590) **actually executed original CURRENT #587 core Git blob `16a7d7043a2ecb59a41bbfb063170235872fd7fc`** with immutable #556 and real W06 in C#9/.NET8. [Original dual-OS Actions #41096343649](https://github.com/C8LyNn/RoboRougelike/actions/runs/41096343649): **15/15 executed source assertions PASS EACH Windows+Ubuntu**, plus separately executed `--release` **EXIT 3 / `HOLD_P0_RED`** on both, meaning independent source release DENIED despite green test detection. Proven ORIGINAL #587 source bugs:
1. Different consumed Light/Heavy world with *same run/generation* accepted, old source retired, generation not incremented. Requires durable checkpoint identity/revision enforcement and exact-source new regression.
2. After post-activation mismatch, candidate `Deactivate()` throws in rollback; `Quarantined=true` but public caller receives `HostException` rather than explicit `RollbackQuarantine`. New source revision must provide unambiguous fatal result or receiver contract, with independent QA of the final R2.

Original previous [#588](https://github.com/C8LyNn/RoboRougelike/pull/588) remains true **historical QA against older #587 core**, but the now-current separate #590 verifies two P0 security REDs against *exact* #587 HEAD; therefore final coordinator status is `#585=HOLD_CURRENT_EXACT_SOURCE_SECURITY_RED`, `#587=HOLD_CURRENT_EXACT_SOURCE_SECURITY_RED`, **#596 R2 SOURCE_QA_PENDING / SOURCE_G0_HOLD**. The modified CLI `--bridge-w16` validates the new #590 result and rejects a fake green or early #596 source receipt. Source/producer compilation or physical transport [B01-40 #594](https://github.com/C8LyNn/RoboRougelike/pull/594) success does not cure these independent release REDs; B01-40 issued physical intake **FORMAL HOLD**, not approved bridge G0.

No new #79 OFFERED, ACK or merge; native Unity PhysX/EditMode/PlayMode NOT_RUN absent activated Editor, full canonical #510 private ledger hydration NOT_IMPLEMENTED, G2 HOLD. Only one selected *lineage* (#587), while a safely corrected/reviewed selected *version* is still pending. The coordinator tool does **not** authenticate GitHub actors by reading its own JSON; original GitHub PR/CI was checked separately.
