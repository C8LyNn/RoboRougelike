# Verification and evidence

1. Original `SOURCE/core.js` must be executed by `SOURCE/test_core.cjs`. Covers deterministic mutable/immutable state semantics, JSON roundtrip, bad schemas, duplicate IDs, path safety, coordinate bounds, XSS-as-text, limit of 100 annotations, and 101 rejection.
2. Actual Chromium opens `SOURCE/index.html` from local files and loads the same original `core.js`; `SOURCE/test_browser.cjs` checks four viewports (320x720, 720x960, 1280x800, 1920x1080): creating labels, DOM-safe notes, exporting/importing JSON via real download/file input, rejection state preservation, pointer drag, toggles, accessibility status text and zero horizontal overflow.
3. CI `.github/workflows/b01-11-frame-ux.yml`: Node core tests on Ubuntu+Windows, Chromium browser on Ubuntu. Artifacts and raw logs will show exact run status. Do not call tests PASS solely because source was authored.
4. Human readability judgment, native Unity 6000.3.14f1 Editor/Player, real screenshots and actual end-user testing are **NOT_RUN**. A synthetic fixture is only a neutral tool demo; it is not real gameplay evidence.

The first owner-producer V8 run executed 30 positive/negative checks against original in-memory `core.js` with 30/30 PASS. This check is narrower than real committed CI; use GitHub CI logs for final per-platform verdict.

## Wave02 / R2 — executable file IO tests (actual performed)
Exact original browser test `SOURCE/test_browser.cjs` now performs **32 assertions per viewport** (4 resolutions), including real browser-generated 640×360 raster PNG upload and pixel check, annotated PNG download with signature/IHDR dimensions/pixel identity, mismatched annotation JSON dimension rejection and state persistence, unsupported MIME rejection with preserved annotation state. Not a dry run. **128/128 PASS** in original Chrome on Ubuntu CI [#40756832894](https://github.com/C8LyNn/RoboRougelike/actions/runs/40756832894). **36/36 core Node PASS on each of Ubuntu + Windows**. Recorded total **200/200** across the three executed jobs; source SHA `506c343ad6af327dac1597c3faf7754050991a3a`.
**Native Unity, real game screenshot capture, actual user study and independent QA remain NOT_RUN.**
