# B01-12 / W62-PASS12 — durable Chromium source-backed replay

**Research-only, not a Unity test and not GitHub Actions.** The purpose of this narrow, executable receipt is to make the earlier W58 PASS11 local ZIP browser evidence independently replayable from GitHub source. It does not add another GUI mock or re-run PASS11's entire 74-case program.

## Immutable input
- Original HTML: `Research/B01-12/W50/D03-critical-warnings.html` (same existing research branch; **not** Codex game UI).
- Corrected original HTML exact Git blob SHA-1: `d5d71cd01c33b544704ea041f887efb1dd32952d`; bytes SHA-256: `e1cd125765c1ce8b625a1fd5036d4a7c3777b612ed43f847dc2a93d215fe8573`.
- Original standalone author replay: `Research/B01-12/W62/reproduce_pass11.py`. It **fails closed** before opening the browser if either original HTML hash does not match.
- Prior PASS11 full creator suite: **74/74** local headless Chromium, with different additional 280/375/768/1024px cases, archived in a conversation-only evidence ZIP; that ZIP is **NOT** a GitHub Actions artifact and its tests have **not** been claimed to run in GitHub CI.

## Re-run on a clean Linux clone / branch
Requires Python 3 and installed Chromium browser at `/usr/bin/chromium`, Python package `playwright==1.57.0`. On a machine under your control:
```bash
python3 -m pip install playwright==1.57.0
git checkout research/b01-12-w50-d03-critical-feedback
python3 Research/B01-12/W62/reproduce_pass11.py > /tmp/b01-12-w62-replay.json
python3 -c 'import json; j=json.load(open("/tmp/b01-12-w62-replay.json")); print(j["passed"], j["total"])'
```
The executable intentionally calls `page.set_content` on the *byte-verified original HTML*, rather than navigating to `file://` (some Chromium sandboxes prohibit local navigation). Source script and test fixture are ordinary repository files, and this command creates only the caller's local result JSON.

## Actual observed verification on 2026-10-27
- Original Linux test host: Python **3.13.5**, Playwright **1.57.0**, Chromium **144.0.7559.96**; true headless Chromium + CDP Accessibility API.
- Ran the published script against original **byte-identical** HTML placed in the matching repository layout.
- Actual **63/63 PASS, 0 failures**; source hashes **2/2**, five new replay viewports **240, 320, 390, 1280, 1920 CSS px** (12 checks each), global zero-JS exception check **1/1**.
- Checks in both treatments: B count accessible while collapsed, inaccessible hidden weapon/mobility answers and moderator controls, original raw CDP AX hidden, Tab and Space focus/disclosure, count and independent reasons on expansion, closing and Escape moderator restoration, A reasons always visible, and horizontal overflow before/after expansion.
- **Independent B01-11** had separately executed corrected **original JS 37/37** source/fake-DOM checks in [#292](https://github.com/C8LyNn/RoboRougelike/issues/292#issuecomment-6454622103); they were **not** Chrome tests. This owner-created replay is **not** an independent B01-11 signoff or a human-screen-reader test.
- The prior full **PASS11 74/74** checks were additionally rerun locally before publishing this replay, on the same exact HTML source and without new GUI changes. Their browser results should not be added to 63/63 as distinct product acceptance.

## Exclusions / handoff
No native Unity Play/Edit, licensed CHL/PARTSIZE G1, PhysX, real screen reader, user observation (0 human participants), measured learning/success or canonical D03 game fix. No change to the original D03 HTML Git blob, original frozen v2, Codex `Assets/**`, CHL #285 or PARTSIZE #286. If a future UI fix changes the HTML, exact hash mismatch is **expected RED** until a separately reviewed test repin; do not silently update the constants.

B01-11 owns independent UX receiver; Codex alone fixes canonical D03. **G0-CHL** re-verification of new producer #285 belongs to B01-20/B01-40, not to this browser replay.
