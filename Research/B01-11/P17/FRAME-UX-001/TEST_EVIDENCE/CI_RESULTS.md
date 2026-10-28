# FRAME-UX-001 — actual executed CI proof and correction history

## Verified producer source at GREEN test run

**GitHub Actions [run #40743816824](https://github.com/C8LyNn/RoboRougelike/actions/runs/40743816824) → overall SUCCESS** on game-repo branch commit `deb833698f642456b44129b1aa50bb7a5ad31c1e` (source code after fixes). Per-job actual results:
- `core (ubuntu-latest)` — **36/36 PASS**, Node22 original `SOURCE/test_core.cjs` against unchanged original `SOURCE/core.js`.
- `core (windows-latest)` — **36/36 PASS**, same source/cases.
- `browser (ubuntu-latest)` — **80/80 PASS**, real Google Chrome/Playwright, 20 DOM/integration assertions at 320x720, 720x960, 1280x800 and 1920x1080. [Browser JSON artifact #12840003347](https://github.com/C8LyNn/RoboRougelike/actions/runs/40743816824/artifacts/12840003347).

## Reproduced RED and fixes
- [Initial real workflow #40743534284](https://github.com/C8LyNn/RoboRougelike/actions/runs/40743534284) core 36/36 both OS PASS; browser **78/80 PASS, 2 FAIL**: 320px horizontal overflow; 1280px asynchronous JSON reimport checked before the state update. Raw failed artifact [#12839604248](https://github.com/C8LyNn/RoboRougelike/actions/runs/40743534284/artifacts/12839604248).
- **Source UX CSS fix** `56b2d378219baca2b12ede939d61e5579574b2ae`: responsive 320px file pickers, full-width control and single-column coordinate fields. **Test synchronization fix** `deb833698f642456b44129b1aa50bb7a5ad31c1e`: wait for async import DOM completion rather than racing the file.text promise.
- Retest above shows GREEN on *all* 4 viewports, not a removed/ignored failing case. All original tests remain enabled.

## Explicitly NOT_RUN
- Unity 6000.3.14f1 native Edit/Play/Windows Player or real in-game UI.
- Actual game-sourced screenshot review, human UX study, independent B01-60 acceptance and B01-40 G2 handoff.
- Other unrelated CI-02/03 may fail Unity license preflight; these **are not** evidence of native gameplay tests.

The result validates **this isolated browser UX tool**, not any game camera, gameplay physics, HUD source or player comprehension.
