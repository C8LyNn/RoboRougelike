# FRAME-UX-001 — B01-11 / PASS17 / screenshot readability review

**Actual browser tool** for reproducible image-anchored UX review. This is a new task distinct from [COMBAT-CUES-001 PR #341](https://github.com/C8LyNn/RoboRougelike/pull/341): the earlier prototype simulates combat feedback, while **FrameUX marks real supplied screenshots**, exports portable normalized annotations and produces annotated PNGs.

## Use
Open `SOURCE/index.html` in Chrome/Edge/Firefox. Browser loads `SOURCE/core.js` from same folder. Initially renders a clearly labelled synthetic placeholder, **NOT an actual Unity screenshot**. Import a local PNG/JPG/WebP image (max 12 MiB, max dimension 8192). Drag on the image to define a region, or set x/y/w/h numbers for keyboard accessible region creation. Assign 5 failure categories, severity P0/P1/P2 and an optional note (max 200 characters). Entries are individually removable; referential 5% margin can be toggled. Export:
- `frame-ux-review.json`: versioned, normalized boxes, screenshot dimensions/name, **no screenshot pixels**.
- `frame-ux-annotated.png`: screenshot with visible labelled boxes.

Import JSON only for an image of identical dimensions. Invalid/out-of-bounds/duplicate/untrusted records fail closed, and existing annotations are preserved on import failure. No network services, upload, analytics or user tracking. Source does not modify Unity at all.

## Source & authority
Owner **B01-11 UX RESEARCH**. This is an isolated source/UI tool under `Research/B01-11/P17/FRAME-UX-001/**`, plus its own `.github/workflows/b01-11-frame-ux.yml`. Main baseline SHA `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. No modifications to `Assets/**`, Codex #310, CHL, PARTSIZE, B01-12 prototypes, or B01-32 damage atlas. **No merge, Codex ACK, native Unity acceptance, or game integration.**

## PASS17 / WAVE02 receiver hardening — R2
Exact executable source remained the same UI/reducer. The original browser test source now additionally exercises a real generated **640×360 PNG** upload via the file control, validates its pixel colors in the imported canvas, exports an actual PNG and validates its PNG signature/dimensions/pixels, refuses invalid image MIME, and refuses previously exported 1280×720 annotation JSON on a 640×360 image. All assertions run at all four target viewport sizes. This is **source/browser verification**, not Unity or human research.

**Actual [CI #40756832894 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40756832894): 128/128 Chromium + 36/36 Linux Node + 36/36 Windows Node = 200/200.** Browser [raw JSON artifact #12845832523](https://github.com/C8LyNn/RoboRougelike/actions/runs/40756832894/artifacts/12845832523). The existing R1 immutable version remains at commit `7e48964a12cfe3a27a4f0680cc136c34e03252dc`; WAVE02 only adds test coverage and receiver evidence. Independent B01-40 / B01-60 acceptance PENDING.
