# B01-31 / A1-GARAGE-ICON-001 (P17-R1)

**Real original 2D asset package — compact icon of the *one* provisionally approved A1 detachable wheel + suspension arm.**

These illustrations are designed for a **future** garage inventory/equipment card: not a new UI implementation, not a game-ready imported Unity Sprite, not a damage state, rarity indicator, material authoring, or mechanical dimensions.

## Actual independent assets

- `A1-garage-icon-dark.svg` — transparent colored icon for a dark garage panel.
- `A1-garage-icon-light.svg` — transparent colored icon for a light garage panel.
- `A1-garage-icon-mono.svg` — monochrome/high-contrast transparent alternative for dark UI.

**All 3 are independently editable 256×256 SVG source images with identical geometric paths** and four semantic subgroups: wheel, support arm, visual damper, **mount proposal**. They are new simplified graphics, **not a byte-identical drawing of the detailed A1 source**. Mechanical/physical validity is UNKNOWN. Colors are exploratory; no HQ final styling decision.

Versioned `manifest.json` includes original A1 provenance, source Git blob identities, receiver/permission gates, file list, output sizes, and safe rollback. Under the [provisional HQ decision](https://github.com/C8LyNn/RoboRougelike/issues/287#issuecomment-6463914298), A1 research art is allowed; **MODEL_JOB is not**.

## Reproducible PNG production (not simulated)

From the root of an isolated clone of the Draft PR branch:

```sh
python -m pip install 'CairoSVG==2.8.2' 'Pillow==11.3.0'
python Docs/B01Concept/P17/A1-GARAGE-ICON-001/render_verify.py
```

This generates **12 actual transparent PNG icons** at 48, 96, 192 and 384 px (3 color treatments × 4 sizes), plus a three-column `A1-UI-contact.png` preview and `receipt.json` with original PNG CRC32, SHA256, dimensions, PASS/FAIL and negative-mutant results. Run the GitHub Actions job in `.github/workflows/b01-31-p17-garage-icon.yml` for an immutable source-SHA-linked artifact.

Acceptance is limited to true image source integrity (same vector shapes across treatments), PNG rasterization, transparent corners, unclipped silhouettes and pixel contrast against the declared **research preview backgrounds**; it is **not actual UI accessibility or human recognition testing**. A black-on-black mutant and a changed wheel-radius mutant must be rejected.

## Review / future import / rollback

1. **B01-40** verifies the exact scoped Draft PR SHA, manifest, and generated original PNG artifact. **B01-60** independently checks source integrity and raster results.
2. **B01-12** may visually review dark/light/monochrome 48/96 px images against the *existing* garage UX research. This package does not alter B01-12 prototype or Unity code; a future recipient must choose a treatment and validate within real game UI.
3. **B01-30** checks continuity with approved A1 visual subject, without inferring a physical mount or creating Model Lab jobs.
4. **Codex** may use these icons only after B01-00/B01-40 handoff, explicit integration approval, and a verified non-overlapping UI owner scope. This Draft is not an INSTALL request or GAME_INTEGRATED claim. No `Assets/**`, prefab, C#, CHL, PARTSIZE, Model Lab, main/develop or existing A1 source touched.
5. **Rollback:** remove only `Docs/B01Concept/P17/A1-GARAGE-ICON-001/**` and `.github/workflows/b01-31-p17-garage-icon.yml` in a disposable checkout. No canonical game change to reverse.

Status: `SOURCE_ART_PRODUCTION / G_DESIGN_PENDING / MODEL_JOB_NOT_AUTHORIZED / NOT_OFFERED / UNITY_NOT_RUN`.
