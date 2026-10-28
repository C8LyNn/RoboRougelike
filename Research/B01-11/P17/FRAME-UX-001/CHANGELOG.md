# CHANGELOG

## P17 WAVE02 R2 — 2026-10-28
- Added **12 original real-browser file IO tests per viewport** (32 x 4 = 128), verifying PNG file upload pixels, annotated PNG export actual bytes/pixels, mismatched JSON size rejection and unsupported MIME state protection; not a gameplay patch.
- R2 original test source commit `506c343ad6af327dac1597c3faf7754050991a3a`, [200/200 executed CI PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40756832894) (128 browser + 36 Ubuntu core + 36 Windows core).
- R1 previously published immutable source SHA preserved: `7e48964a12cfe3a27a4f0680cc136c34e03252dc`. WAVE02 no changes in game or UI reducer.


## P17 R1 — 2026-10-28
- Original standalone screenshot annotation/review browser app, independently scoped from Combat Cues.
- Pure fail-closed deterministic `FrameUXCore` reducer: rectangle validation, local JSON import/export, immutable operations, severity summary and user data limits.
- UI provides local screenshot file import, clickable/drag annotations, keyboard-accessible normalized fields, individually removable labels, reference zone overlay, synthetic starting fixture and portable JSON/PNG export.
- Original adversarial Node and real Chromium test sources, dedicated isolated GitHub Actions workflow.
