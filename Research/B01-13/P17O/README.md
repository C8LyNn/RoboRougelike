# B01-13 / PASS17 WAVE17 — OFFLINE REVIEW BOARD 001/R1

Owner: B01-13 HUMAN PLAYTESTING. Dispatch #608, ownership #276.

## Functional deliverable — new, nonduplicative USER INTERFACE
This is a working **single-file offline HTML dashboard** for an existing W16 cross-observer report, not another data collection tool, Save contract, host bridge or Unity test runner. It creates a visual queue of cases where two technical operator transcripts disagree. There are native browser keyboard-accessible priority, scenario, flag and ID filters, Escape-to-clear search, reset, print layout, a compact provenance panel, and a prominently displayed UNVERIFIED evidence warning. All processing is local and no external resources or network are loaded. Generated HTML is not committed: the original structured JSON and any private technical data remain outside Git.

Distinct from W12 scheduling #531, W13 part-state comparison #539, W14 image SHA256 #569, W15 timing analysis #573, and W16 cross-rater scoring #595. W17 is the **practical visual review surface for human QA operators**, not another source test harness for Save/Load. It uses **original W16 JSON export only** and never edits frozen W16 sources.

The one authoritative frozen reattach source is #510 SHA d94400d95d13db7f27e1533df05c9292d5380431; sole Save schema #556 SHA 33094f154c7b91119b36d58aab285e6e83c5de57. W16 original producer #595 exact tested SHA 3f83937874c69cb8744247cb51606dcb2fa0fd91. Bridges #596/#604 are under B01-23/62 release decision, **not installed, imported or selected by this B01-13 tool**. #511/#494 and older Save schemas remain HOLD.

## Run it without internet
Prerequisites: Node.js 20+ on a private approved host, a **fully generated original W16** rater-consensus-result.json from PR #595, and a local writable private directory *outside* any Git working tree.

    node Research/B01-13/P17O/review_board.mjs --input /private/w16-result.json --output /private/review-board.html

The input must be an original complete B01-13-RATER-CONSENSUS-RESULT/R1 with the exact selected game/Save pins, synthetic or genuinely operator-recorded technical observations, original W15 stamps, source-only flags, and internally consistent original summary, review queue, status/flag calculations and agreement. Missing fields, arbitrary free text, PII, tampered summary or forged native/human PASS fail closed. You cannot score raw W15 trials directly; generate W16 result using frozen W16 package first. Output file must not exist, is created exclusively with fsync/mode 0600 where supported. Do not run it inside the repository or commit any actual records.

Open review-board.html in a browser, even offline. It presents all original rows with a **non-claiming review priority**: P0 = disagreement about completed recovery or need for help; P1 = unobserved notice or inconsistent feedback classification; P2 = elapsed-time/attempt discrepancies. These flags are **review scheduling heuristics**, never actual P0 game bugs. CONSISTENT means two transcriptions appear aligned, not that a test passed. Display all or filter by scenario, flag, priority or anonymous trial ID. Print layout reveals all rows for discussion.

The exporter validates the *full original* W16 report, including the declared SHA pins, no participant identity fields, timestamp differences, count and flag tallies, actual Cohen kappa calculation and review queue order. It binds the exact physical JSON bytes to a SHA256 shown in the exported dashboard so observers can cite the original private file. It never changes the original report or raw observations.

### Security / usability
No external scripts/images/fonts or fetch. Inline JS has a generated SHA256-only CSP allowlist. HTML text is encoded and input fields are strict safe enums/numeric counts. The filters use DOM APIs rather than untrusted JS expressions. Semantic tables, labels, focus rings, status live-region, print CSS and reduced-motion support are provided. The exact browser behavior has **Node vm DOM-simulation test only**, not a real headless-browser test; visual WCAG compliance and real human comprehension have not been independently established. Windows ACL and symlink junction hardening are unverified. Physical EXE SHA256 in W16 is only a reported hash and not attested to canonical Unity source.

### Honest gates
The generated report always includes warnings: HUMAN_VERIFIED=0, UNITY_G1=NOT_RUN, LIVE_SAVELOAD=NOT_VERIFIED, source_to_player=UNVERIFIED, G2=HOLD. Neither synthetic operator transcripts nor their review visualization qualify as native Unity or actual human testing. If a later real study is authorized it must obtain informed consent, observe retention rules and establish real Unity Player provenance first. Actual human participant sessions in this package = ZERO.

## Run exact original source tests
Dedicated CI (Windows2025/Ubuntu24.04 x Node20/22) authenticates original W16 #595 and original W15 #573 by Git blob, then **uses those unchanged original real source files to generate report fixtures**, produces physical offline HTML, runs positive and adversarial cases and simulates filter interactions in an isolated mock DOM. This is NOT full browser automation. The physical manifest pins five own source/docs/workflow blobs and their byte lengths and recalculates SHA256 per runner.

    python3 Research/B01-13/P17O/verify_package.py
    B01_W16_ORIGINAL_SOURCE=/private/PinnedW16.mjs B01_W15_ORIGINAL_SOURCE=/private/PinnedW15.mjs node --test Research/B01-13/P17O/test_review_board.mjs

Ownership: Research/B01-13/P17O/** plus dedicated .github/workflows/b01-13-p17w17-review-board.yml. No Assets/**, no selected bridge game / Codex / earlier frozen B01-13 files. No merge or unauthorized live game integration.

Receiver B01-40: exact physical scope/Git manifest and safe rollback G0. B01-60: independent reproduction of original source CI on exact SHA; B01-00: evaluate later authorized playtest adoption with consent. Rollback = close unmerged isolated Draft and remove privately generated HTML only under approved privacy/retention SOP. Native Unity G1 NOT_RUN, independent source G0 PENDING, real human studies 0, G2 HOLD, Codex ACK NONE.
