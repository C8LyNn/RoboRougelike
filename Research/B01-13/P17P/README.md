# B01-13 / PASS17 WAVE18 — REVIEW-DECISION-HANDOFF-001/R1

**Owner:** B01-13 Human Playtesting. Dispatch #632, governance #276. Entirely offline.

## What actually works
A new, executable **reviewer-decision handoff UI** extending (without modifying or copying its scoring algorithm) the original immutable W17 offline review board #615. After validating the exact original technical observer JSON via imported frozen W17 functions, this package adds labeled decision controls for unresolved disagreements and a local JSON download button. This is the practical follow-up after W17's read-only queue; not a new game Save/Load schema, another test harness for #596, or another observer metric. All technical observations remain unverified.

Decisions have four strict meanings: PENDING = no review yet, CHECK_TRANSCRIPT = proposed data check, REQUEST_NATIVE_REPRO = proposed native verification, DOCUMENT_VARIANCE = proposed documentation of observer discrepancy. **No decision denotes performed QA, accepted gameplay, verified human participation or Codex approval.** Export is local from browser Blob, no network, cookies or localStorage. Decisions are ephemeral until the user downloads the file; no participant names or free-text fields. Export fields remain human_verified=0, native_unity=NOT_RUN, live_game_save_verified=false, codex_accepted=false, source_report_sha256 and original W17 SHA.

## Original W17 confirmed RED and repair strategy
Historical immutable W17 original #615@f05790e274f31ac39c6ae4b2e873db896a3b36d8, real CI run #41101319952, Ubuntu/Windows x Node20/22: **44 PASS / 1 FAIL each**, manifest 5/5 each. Its single negative test assigned "tampered" Cohen kappa=0.5, but original fixture **genuinely produces exactly 0.5**, so no rejection is expected. W18 new tests (without touching prior source/test/commit) prove true original 0.5 and a real tamper value 0.1234 fails with KAPPA_DRIFT. Original W17 source remains unchanged and historical RED remains visible; W18 is a distinct versioned functional extension, not false W17 PASS.

## Frozen exact dependencies
- #510 only game d94400d95d13db7f27e1533df05c9292d5380431
- #556 only Save schema 33094f154c7b91119b36d58aab285e6e83c5de57
- #596 only host efb2b06eafedab5469b57f163152d96aca3c5915
- #604 ALT_HOLD, never imported
- original W17 #615 commit f05790e274f31ac39c6ae4b2e873db896a3b36d8, original review_board.mjs Git blob d9f4267ad2f827a84e08f63e4c5880af1b8c739e
- original W16 rater_consensus.mjs blob 56d158bb6c107a04fc42e7ee78d6a07317997729, W15 recovery_timeline.mjs blob 6ec383f0b3e341d58f9b5e971fc435a74d78a583.

## Private CLI usage
Node20+; original valid W16 technical report must contain at least one disagreement, with no forged human/Unity attestation. Store input and output outside Git.

Fetch unmodified original W17 into private location (binary-safe command in a checkout):
    git show f05790e274f31ac39c6ae4b2e873db896a3b36d8:Research/B01-13/P17O/review_board.mjs > /private/PinnedW17.mjs

Run:
    node Research/B01-13/P17P/decision_handoff.mjs --input /private/W16_report.json --upstream /private/PinnedW17.mjs --output /private/triage.html

Open generated HTML offline. Its original W17 filters and print function remain available. New panel offers an export button that downloads a purely technical JSON decision proposal bound by SHA256 to the exact source JSON bytes. CSP SHA256 allowlist is expanded to include new inline script, original W17 stays unchanged. Input original W17 validator rejects PII, counterfeited flags/kappa/queue and forged Unity/human status. Original W17 source Git blob is authenticated before import. No cross-system signals or automatic integration. Output exclusive-create/no-overwrite with fsync and 0600 where supported; output inside a Git ancestor refused. No claim about Windows ACL and junction hardening.

## Actual tests and receiver
Dedicated source CI retrieves exact original W15/W16/W17 source files by Git commits, Git blob verifies physical bytes, then executes W18's original Node regression cases with original upstream. Tests cover original W17 RED correction, all four decisions, DOM simulation for local export, invalid dropdown fallback, CSP hashes, physical subprocess HTML, no-overwrite and tampered input. Mock DOM tests are **NOT real browser automation**; no human or Unity tests. Matrix Windows2025/Ubuntu24.04 x Node20/22. Independent manifest pins five owned production/test/doc/workflow Git blobs and physical lengths and prints SHA256.

Scope only Research/B01-13/P17P/** plus dedicated W18 Actions workflow. No Assets/**, no B01-23/62 adapter changes or protected branch merge.

Six independent lifecycle fields must be reported. Until independently done: G0_B01-40=PENDING, G1_NATIVE_UNITY=NOT_RUN, LIVE_GAME_SAVELOAD=NOT_VERIFIED, Codex ACK NONE, integrated NO, released NO, human participants ZERO. Receivers B01-40 for exact source/rollback, B01-60 for independent replay, B01-00 for authorized playtest setup. Rollback abandon isolated unmerged Draft; private records follow approved retention and consent SOP.
