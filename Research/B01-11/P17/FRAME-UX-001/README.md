# FRAME-UX-001 — B01-11 / PASS17 / screenshot readability review

**Actual browser tool** for reproducible image-anchored UX review. This is a new task distinct from [COMBAT-CUES-001 PR #341](https://github.com/C8LyNn/RoboRougelike/pull/341): the earlier prototype simulates combat feedback, while **FrameUX marks real supplied screenshots**, exports portable normalized annotations and produces annotated PNGs.

## Use
Open `SOURCE/index.html` in Chrome/Edge/Firefox. Browser loads `SOURCE/core.js` from same folder. Initially renders a clearly labelled synthetic placeholder, **NOT an actual Unity screenshot**. Import a local PNG/JPG/WebP image (max 12 MiB, max dimension 8192). Drag on the image to define a region, or set x/y/w/h numbers for keyboard accessible region creation. Assign 5 failure categories, severity P0/P1/P2 and an optional note (max 200 characters). Entries are individually removable; referential 5% margin can be toggled. Export:
- `frame-ux-review.json`: versioned, normalized boxes, screenshot dimensions/name, **no screenshot pixels**.
- `frame-ux-annotated.png`: screenshot with visible labelled boxes.

Import JSON only for an image of identical dimensions. Invalid/out-of-bounds/duplicate/untrusted records fail closed, and existing annotations are preserved on import failure. No network services, upload, analytics or user tracking. Source does not modify Unity at all.

## Source & authority
Owner **B01-11 UX RESEARCH**. This is an isolated source/UI tool under `Research/B01-11/P17/FRAME-UX-001/**`, plus its own `.github/workflows/b01-11-frame-ux.yml`. Main baseline SHA `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. No modifications to `Assets/**`, Codex #310, CHL, PARTSIZE, B01-12 prototypes, or B01-32 damage atlas. **No merge, Codex ACK, native Unity acceptance, or game integration.**
