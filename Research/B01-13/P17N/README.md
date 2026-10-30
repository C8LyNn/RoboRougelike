# B01-13 RATER-CONSENSUS-001/R1 — PASS17 WAVE16
Owner: B01-13 HUMAN PLAYTESTING; dispatch GitHub #589, ownership #276.

## NEW ACTUAL FUNCTION
Offline original Node20+ **blind two-operator timeline comparison**. For each identical anonymous trial the tool checks original independently collected W15 recovery timelines using actual byte-exact W15 score() and calculates disagreement in completion/recovery category, assistance, feedback quality, time-to-notice, total duration and attempts. It produces actionable conflict reason labels and cohort completion-status Cohen kappa, including mathematically undefined kappa on constant categories. It never adjudicates truth or attributes tests to people.

Unlike W12 #531 (balanced Light/Heavy), W13 #539 (visual module comparisons), W14 #569 (physical PNG binding) and W15 #573 (one-operator recovery metrics), W16 examines **reliability between two independently recorded operator observations**. Not a competing gameplay or Save/Load test harness.

Frozen original source pin: B01-13 W15 PR #573, exact head acc2288f72d47bd3969f840524cf8c5d07c0c6d2; original Git blob 6ec383f0b3e341d58f9b5e971fc435a74d78a583 for Research/B01-13/P17M/recovery_timeline.mjs.
Only selected Secure Reattach #510 exact d94400d95d13db7f27e1533df05c9292d5380431. Only Save contract #556 exact 33094f154c7b91119b36d58aab285e6e83c5de57. No alternative or host bridge sources installed.

## PRIVATE CLI
Make input JSON in private storage outside any Git repo. It must contain exactly:
{"schema":"B01-13-RATER-CONSENSUS/R1","truth_gate":"UNATTESTED_TECHNICAL_TRANSCRIPTION","pairs":[{"a": ORIGINAL_W15_FILLED_TRIAL_JSON, "b": ANOTHER_ORIGINAL_W15_FILLED_TRIAL_JSON}]}
The capitalized placeholders mean replace with complete valid original W15 objects. There is no extra operator ID, name, free text, PII, invented Player or Unity claim. Both W15 trial objects must be for the same anonymous trial ID, scenario, physical Player EXE SHA256. Different trials in one document must have unique IDs and the exact same EXE hash. At least 1 pair, at most 128.

Get original W15 source bytes from immutable commit (example, do not change or reformat the source bytes):
git show acc2288f72d47bd3969f840524cf8c5d07c0c6d2:Research/B01-13/P17M/recovery_timeline.mjs > /private/PinnedW15.mjs

Run from a checkout containing W16 source:
node Research/B01-13/P17N/rater_consensus.mjs --input /private/pairs.json --upstream /private/PinnedW15.mjs --output /private/review.json

The original source is verified by physical Git blob SHA1, one-byte changes fail closed. A private output file is created only if absent (O_EXCL), fsynced, mode 0600 where supported. Git ancestor output refused. Results include physical EXE hash *claimed by source timeline*, completion-status disagreement, per-pair reasons, absent notice, help discrepancy, feedback classification discrepancy, and cohort agreement statistics.

## RULES / LIMITATIONS
Conflict if mismatched RECOVERED/ABANDONED/STOPPED, mismatched assistance, one missing NOTICE, notice timestamp difference over 1000ms, completion-time difference over 2000ms, action count difference >1 or changed presence of CLEAR/AMBIGUOUS/ABSENT feedback. These are *heuristic review prompts*, not verified thresholds or statistical usability acceptance gates. Review queue never silently rewrites either observation. Cohen kappa is null if chance agreement equals 1, not a fabricated perfect score.

No real human trial has been performed. Actual playtests require authorization, consent and appropriate storage, true Unity Player source-to-build attestation and legal activation. Output always marks human_verified=0, native_unity=NOT_RUN, live_save_load_verified=false, source_to_player=UNVERIFIED and code_ack=false; real Unity G1, actual gameplay, internal Save IDs, secure rollback and G2 are NOT VERIFIED. Test data is wholly synthetic. A matching two-operator record does not prove a real game save, and differing records do not prove a game bug. Windows ACL/junction guarantees not independently checked.

## REAL QA AND HANDOFF
Execute with env variable B01_W15_ORIGINAL_SOURCE pointing to exact original frozen source:
node --test Research/B01-13/P17N/test_rater_consensus.mjs
python3 Research/B01-13/P17N/verify_package.py
Dedicated Actions: Ubuntu24.04 and Windows2025 x Node20/22. Original Node source tests use physical tempfile/subprocess and corrupted-source negative tests; source manifest audits five physical Git blob/byte pins and SHA256 in every runner. Source QA passing != independent receiver G0 or native Unity.

Owned files: Research/B01-13/P17N/** + .github/workflows/b01-13-p17w16-rater-consensus.yml only. No Assets/**, frozen #510/#556, Codex gameplay or other-owner source; previous B01-13 #573 unchanged.
Receivers: B01-40 exact SHA source G0 + rollback; B01-60 independent source reproduction; B01-00 study approval. No Codex ACK, offer or merge. Rollback: abandon unmerged Draft and dispose privately collected data only per retention/withdrawal policy.
