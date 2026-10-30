# B01-13 — DAMAGE-CUE-TUTORIAL-001/R1 — PASS17 / WAVE19

**Owner:** B01-13 HUMAN PLAYTESTING. Dispatch #646. Exclusive isolated paths Research/B01-13/P17Q/** plus one dedicated workflow. No canonical Assets/** or merge.

## Deliverable: real playable UX mockup, not another test recorder
An original **offline single-file interactive HTML game-like tutorial** helping players recognize three robot part conditions: OK/SPRAWNA, DAMAGED/USZKODZONA, DETACHED/ODERWANA, by **redundant signal glyph, line-border style and text**, never color alone. Six deterministic original rounds alternate LIGHT/HEAVY and each target each condition twice. Four interactive part buttons per round, exactly one correct. Feedback says why an answer was right/wrong. Progress, restart, completion score, keyboard 1–4 choice, N next, M toggle full-label/symbol-only presentation, R restart, native Tab/Enter and aria-live responses. Responsive 2/4-column board with keyboard focus and print/reduced-motion handling.

This is **distinct** from W12 trial matrix #531, W13 visual save comparisons #539, W14 witness PNG #569, W15 recovery-event score #573, W16 cross-rater stats #595, W17 offline discrepancy board #615 and W18 disposition export #639. W19 is the first actual playable interactive **visual damage-readability practice** in B01-13; it neither captures telemetry nor claims human testing. No actual Unity import/PhysX or live gameplay state.

## Usage
Requires Node 20+ (stdlib only). Output into a **private, writable directory outside a Git repository**:

    node Research/B01-13/P17Q/damage_cues.mjs --output /private/damage-cue-tutorial.html

Open the generated HTML file locally in a current browser (no internet). The entire game-like microtutorial is self-contained: no third-party scripts, fonts, images, requests, cookies, storage, telemetry, event upload or remote tracking. The static hand-authored rounds are deterministic; progress is purely ephemeral in memory and lost on refresh. HTML has a content-security-policy pin of the one inlined original script, default-src none and connect-src none.

**Interaction:** answer each task by clicking one robot part / pressing keys 1-4; only one answer is scored per round. Continue with N/Next, switch text labels with M, restart R. Each part exposes an accessible aria-label with the state, including when text labels are visually hidden. Correct / incorrect is also spoken through aria-live. This prioritizes understanding and discoverability, **not a blind study**. Full legend is deliberately shown: this is *training*, not measurement.

State semantics in this prototype are illustrative, NOT the real game's implementation of destroyed parts, repair economics, saving, consumed ID or physically detached rigidbodies.

## Truth boundary
At this snapshot, **0 actual human participants**, **G1_NATIVE_UNITY=NOT_RUN**, **LIVE_SAVELOAD=NOT_VERIFIED**, **VISUAL_ACCESSIBILITY_CERTIFICATION=NOT_RUN**, and **G2=HOLD**. Node vm simulation tests are not actual native browser end-to-end tests. No actual physical robot gameplay was performed. The HTML is not a shipped Unity scene and does not assert actual player usability outcomes.

Immutable baseline: original selected gameplay #510@d94400d95d13db7f27e1533df05c9292d5380431; Save schema #556@33094f154c7b91119b36d58aab285e6e83c5de57; selected R2 host #596@efb2b06eafedab5469b57f163152d96aca3c5915. Alternative #604 ALT_HOLD. R3 #642/#644 selection belongs exclusively to B01-23/62 and independent QA/receiver, **not decided or imported by B01-13**. No source import from any selected core. No Codex-owned camera, weapons, Garage, MOV009 or integration file edits.

## Original source QA
Dedicated workflow: Windows2025/Ubuntu24.04 × Node20/22. Each runner first validates five actual original scoped file byte counts and Git blob SHA1 + prints SHA256 through original verify_package.py. Then executes 41 Node original source tests, including strict model invariants, all 6 rounds, foreign inputs, physical exclusive private HTML generation/CLI and live original inline script executed via deterministic **Node vm mocked DOM** (keyboard, clicks, score, mode, restart, full completion). This does not substitute genuine visual/browser/Unity/human testing. Only **executed exact-head logs** qualify as tests passed.

    python3 Research/B01-13/P17Q/verify_package.py
    node --test Research/B01-13/P17Q/test_damage_cues.mjs

## Receivers and rollback
B01-40: independent scope/hash/source-only G0 and non-destructive rollback review. B01-60: independent exact-head source and actual real browser/Unity when possible; B01-00: schedule future consented human comprehension tests with a genuine Unity gameplay binary and compare against this UX mockup. No Codex OFFER/ACK, integration or release is implied.

Rollback: abandon unmerged Draft/branch. Never install prototype into canonical game without an explicit owner decision. Generated local HTML may be deleted safely when no longer needed; no participant data is collected.
