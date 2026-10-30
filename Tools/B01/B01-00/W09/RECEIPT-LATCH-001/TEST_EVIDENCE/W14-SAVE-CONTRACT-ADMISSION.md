# B01-00 / PASS17 WAVE14 — one Save/Load contract, no premature intake

**Owner scope:** B01-00 coordination/queue only. Does not select source for B01-23, edit B01-23/B01-61/B01-62/ B01-40/B01-60 files, freeze #510, integrate, merge, or touch the Codex-owned Unity game.

## Actual immutable original GitHub evidence read (not invented QA)

- One gameplay producer [#510](https://github.com/C8LyNn/RoboRougelike/pull/510), original SHA `d94400d95d13db7f27e1533df05c9292d5380431` unchanged. #511/#494 remain HOLD alternatives.
- **Contract A** [#544](https://github.com/C8LyNn/RoboRougelike/pull/544) @ `e77529262b8f03c6e9f0e25b642038c351872044`: producer source C# tests completed SUCCESS on Windows and Ubuntu [#41082219558](https://github.com/C8LyNn/RoboRougelike/actions/runs/41082219558), but the independent #550 tests are NOT for #544. **Independent exact-HEAD source QA and owner exclusive selection NOT_RUN/PENDING**. Do not call producer PASS independent acceptance.
- **Contract B** [#549](https://github.com/C8LyNn/RoboRougelike/pull/549) @ `6da2c523e03766cd5a6deaed0cb3cccf8765354b`: producer source CI [#41082728707](https://github.com/C8LyNn/RoboRougelike/actions/runs/41082728707) SUCCESS, but the independent [#550](https://github.com/C8LyNn/RoboRougelike/pull/550) QA SHA `be2f8e76a881481c904ecc368c17218c80bd1709`, actual [#41082794585](https://github.com/C8LyNn/RoboRougelike/actions/runs/41082794585), executed real original C# + original W06 and reproduced **two expected SECURITY_RED defects**: caller mutates validated `RunSnapshot.Parts` after publication; public `ColdRestore.GetLedger` exposes a mutable real W06 ledger allowing consumed-ID rearm with no physical dock. Successful QA workflow means the **RED witness succeeded**, not a release gate PASS.
- **Important independent source-blob check performed with GitHub connector:** fetched real `Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs` on QA-tested commit `c2c179317af8155d17a1027283fe991565779ede` and current contract #549 HEAD `6da2c523e03766cd5a6deaed0cb3cccf8765354b`. Both returned exactly the same original Git blob **`6c036985411164372bf3d21d467780ff3777544e`** (13,223 JS UTF-16 string units). Thus the independent RED applies to unchanged original vulnerable C# bytes. The new W14 offline gate pins the two commit IDs and this shared blob explicitly: **it does not falsely claim QA executed at the later branch HEAD**.
- W13 source-only Codex [Inbox #79 comment 6526629723](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6526629723) already exists for #510 **alone**. It cannot be promoted into SaveLoad accepted, native G1 or ACK.

## New real executable control

`save_admission.py` is an isolated stdlib Python audit of `save_contract_w14_hold.json`. It insists on the exact frozen #510, exactly the two named Save contract candidate PRs #544/#549, immutable commit and original blob references, source producer vs independent QA **as distinct states**, unique B01-23 owner selection with canonical decision reference, and eight still-active integration HOLDs (Unity, real host SaveLoad, migration, original unsafe rollback, G2, Codex ACK, integration, merge). Fail-closed checks reject ambiguous selection, old PASS replay on a newer SHA, unproven older RED transfer if source blob changes, duplicate candidates and attempts to impersonate independent review.

Real original result of this current evidence:
```json
{
  "candidate_statuses": {
    "544": "HOLD_INDEPENDENT_SOURCE_QA",
    "549": "REJECT_EXACT_SOURCE_SECURITY_RED"
  },
  "owner_choice_state": "UNDECIDED",
  "only_source_candidate_for_manual_review": null,
  "verdict": "HOLD_NO_ACCEPTED_SINGLE_SAVE_CONTRACT"
}
```
A hypothetical later separate B01-23 exclusive selection + exact-HEAD independent QA PASS can produce only `MANUAL_G0_REVIEW_REQUIRED`, **never automatically ACCEPTED, OFFERED, CODEX_ACK or INTEGRATED**. This is a consistency policy, NOT a GitHub-authenticated identity check.

## Actually executed original W14 tests

- Original 53 new positive and adversarial unittest cases in `test_save_admission.py`, total original tool suite **225** with preserved 172 W09–W13 tests.
- [Source GitHub Actions #41087521336](https://github.com/C8LyNn/RoboRougelike/actions/runs/41087521336) completed success on both Windows2025 and Ubuntu24.04: **225/225 PASS per OS**, including original actual `save_admission.py` invoked against the pinned W14 evidence, returning correct HOLD, and original online PR427 workflow/runner evidence check.
- This is an executable *source-only coordinator policy*, **not a live Unity Save/Load test** and not an independent re-execution of #550 vulnerability tests. B01-61 owns that separate real C# evidence.
- Current final manifest-HEAD Actions must be checked after manifest commit.

**Delivery/rollback:** isolated owner `Tools/B01/B01-00/W09/RECEIPT-LATCH-001/**` and its preexisting dedicated workflow. No GitHub mutation from the Python program; neither script modifies a repo, source game, physical package or Inbox. To rollback, discard this unmerged coordination tool and disposable reports, preserving frozen producer/QA/source-only offer evidence. Native Unity G1 NOT_RUN due activation in global CI; real host migration, safe receiver adoption and canonical Save/Load NOT_DONE.
