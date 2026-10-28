# B01-31 — A1-VIEW-ICONS-001 / P17-W02-R1

**Three new original vector perspectives of one provisionally authorized A1 wheel+support-arm module**, intended only for small visual orientation cues in future garage/part-detail UI.

Unlike [B01-31-A1-GARAGE-ICON-001 PR #358](https://github.com/C8LyNn/RoboRougelike/pull/358), which shows one pose across three color palettes, this package has **three different drawn viewpoint compositions** (`face`, `side`, `top`) in a single coherent color language. Not a 3D CAD model; all physical dimensions, actual socket, pivots, collision and clearance remain unmeasured.

## Files and reproducibility

- `A1-orientation-face.svg`: original front-wheel/face visual with abstract bracket.
- `A1-orientation-side.svg`: original side visual with twin beam support and illustrated damper.
- `A1-orientation-top.svg`: original overhead arrangement, illustrated mount and wheel tread.
- `manifest.json`: package version, source SHA-1 pins, origin reference, receiver policy and no-go gates.
- `render_verify.py`: original-source vector tests, actual transparent PNG export at 48/96/192/384, contact sheet, SHA256/CRC32 of every PNG, and three mutation-negative controls.

Run in a **disposable checkout** at the PR commit:

```sh
python -m pip install --disable-pip-version-check 'CairoSVG==2.8.2' 'Pillow==11.3.0'
python Docs/B01Concept/P17/W02/A1-VIEW-ICONS-001/render_verify.py
```

Expected: `status=PASS, fail=0, png_files=13`; PNG files appear in `rendered/`, along with a verifiable `receipt.json`. The branch's own GitHub workflow runs this script and publishes an original PNG ZIP artifact.

## Review, receiver and rollback

1. B01-40: verify exactly scoped paths, expected SHA, manifest/source/file identities, archive and rollback.
2. B01-60: independent source/PNG QA; **producer's green tests are not independent acceptance**.
3. B01-12: optionally compare the 48- and 96-px images in an actual garage research context. **No human user test has been performed**; this package does not modify B01-12's prototype or define the game UI.
4. B01-00 → Codex: conditional intake after independent QA, approval and a deliberate implementation slot; **no direct `Assets/**` installation or gameplay changes requested**. Codex remains sole owner of integrated Unity source, all merges and game acceptance.
5. Rollback in disposable checkout: delete only `Docs/B01Concept/P17/W02/A1-VIEW-ICONS-001/**` and `.github/workflows/b01-31-p17-w02-view-icons.yml`. Neither the original A1 PR #294 nor previous B01-31 PRs #334/#358 are modified.

**Known limits:** provisional G-CONCEPT research authorization [#287](https://github.com/C8LyNn/RoboRougelike/issues/287#issuecomment-6463914298) is **not** G-DESIGN, final style selection, UNIT/PARTSIZE approval, native Unity G1, a MODEL_JOB or integration authorization. The three drawings are stylized viewpoints rather than dimensionally consistent orthographic engineering views.

**QA classification:** source/raster G0 only. `UNITY=NOT_RUN; PHYSX=NOT_RUN; HUMAN=NOT_RUN; MODEL_JOB=NOT_AUTHORIZED; GAME_INTEGRATED=NO`.
