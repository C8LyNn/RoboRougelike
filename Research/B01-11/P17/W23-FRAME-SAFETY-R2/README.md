# B01-11 / PASS 17 WAVE 23 — FRAME-SAFETY-001/R2

## Confirmed real W22 UX defect; narrow original R2 source fix
The old immutable [W22 source PR #726 @ c27fd4ecef6df52b0b8086cc2234904918fa1c4b](https://github.com/C8LyNn/RoboRougelike/pull/726) labels its button **"Przywróć kamerę"** yet the actual JavaScript handler calls `scene=fresh()`, turning both player's LPM and PPM weapon modules back on and resetting vertical FOV 70°/40° to 55°. It only preserves display dimensions. This is a behavioral source bug, not a styling opinion, and falsely changes two independent user design choices.

R2 uses exactly one **camera-only** reset: `scene.camera={...fresh().camera}` while preserving screen size, FOV, both active weapon flags and their `aria-pressed` state. A new, distinctly labeled **"Zresetuj całe doświadczenie"** button intentionally restores the entire original state, including wide 900×506 format, both equipped weapons, FOV55 and camera. The old W22 branch and exact SHA are not changed.

## Real executable original-source and real browser verification
- Original W22 70-case Node22 source tests run unchanged against R2 and byte-exact W22 manifest updated solely for patched HTML.
- W23 `test_source.cjs` checks exact immutable historical original Git blob SHA and the original reset defect (a negative control), R2 handler isolation, new full reset, W22 core unchanged, and both SHA256 manifests.
- W23 `test_browser.cjs` launches **real headless Chromium** with Playwright (not synthetic DOM). It loads original W22 R1 **directly from the pinned Git SHA**, confirms original expected-RED destructive reset on actual browser interactions, and tests R2 safe reset/full reset across landscape and portrait, independent left/right weapons, 40° FOV, keyboard Enter/Space, 50-toggle stress, real canvas module count and no page errors. Browser source/fixture is inlined into the test page; no third-party website or internet API is visited during interaction.
- Dedicated GitHub Actions Ubuntu24/Windows2022 Node22 source checks plus separate genuine Ubuntu24 headless Chromium job. Browser test uses a pinned Playwright npm package only on the ephemeral GitHub-hosted runner. Source checks are independent from a browser sandbox failure.
- A manifest of six source artifacts pins exact SHA256 and byte lengths, while W22's five-source manifest is re-pinned to R2 HTML. No manifest self-inclusion or generated binary in repository.

Run in an isolated clone at R2 HEAD:
```
node --test Research/B01-11/P17/W22-FRAME-SAFETY-001/test_frame.cjs Research/B01-11/P17/W23-FRAME-SAFETY-R2/test_source.cjs
npm install --no-save --ignore-scripts playwright@1.56.1
npx playwright install chromium
node --test Research/B01-11/P17/W23-FRAME-SAFETY-R2/test_browser.cjs
```
These are **headless Chromium UI checks, not Unity/PhysX or consented human playtests**. R2 is still a synthetic 3D perspective experiment: it cannot claim 3D occlusion correctness, actual camera physics, FPS, native gameplay integration, or end-user usability without B01-20/13/60.

## Branch and authority boundary
New separate Draft R2 **stacked solely on frozen B01-11 W22 #726 exact SHA**, no forced update or original W22 branch change. Only B01-11 original HTML+old manifest on the R2 branch plus new W23 B01-11 test, runbook, manifest, and own workflow. No `Assets/**`, `main`/`develop`, #510/#556/#596/#642/#647, selected R5 #690, chosen canonical #729, #712/#669, or any ALT_HOLD source touched. No merge.

**Receivers:** B01-12 and B01-20 for further authorized UI camera/robot work; B01-13 for later human testing; B01-40 for original path/manifest/intake; B01-41/61 for independent source regression; B01-60 for native Unity6000.3.14f1 when user chooses to run an activated editor. B01-00 for queue/handoff.

**Six states:** IMPLEMENTED=YES; TESTED=SOURCE_OR_BROWSER_ONLY_WHEN_EXECUTED; QA_ACCEPTED=NO; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO. SOURCE_G0=PENDING, NATIVE_G1=G1_NOT_RUN, CANONICAL_LIVE_SAVE=NOT_APPLICABLE, CODEX_ACK=NONE, G2=HOLD, INTEGRATOR_FEEDBACK=NOT_MEASURED.

**Safe rollback:** close Draft and drop the own R2 child branch, leaving immutable W22 and all frozen paths unchanged. No shared-folder deletion, no Unity project modification.
