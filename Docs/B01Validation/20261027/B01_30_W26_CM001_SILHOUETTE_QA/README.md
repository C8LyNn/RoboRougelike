# B01-30 / W26 — independent CM001 A/B/C readability stress (QA/research only)

**Task:** `B01-30-W26-CM001-64-128-READABILITY` per dependency queue [#297](https://github.com/C8LyNn/RoboRougelike/issues/297). Not a new model, runtime system or alternative producer. **B01-31 retains exclusive ownership** of [PR #294](https://github.com/C8LyNn/RoboRougelike/pull/294). HQ has **NOT SELECTED** A/B/C; `ART.A2 BLOCKED_BY HQ_SELECT/MIX/REVISE/PARK`, hence TECH_ART_CARD/G-DESIGN/MODEL_JOB remain blocked.

## Actual origin / upstream dependency already updated

Source producer latest reviewed SHA `3efd76bb2eaa319282a51ad588f8f4176d0cc33b`. Original A/B/C SVGs are **byte-for-byte unchanged since** `62b9b6660819ff71ad007843802ac0e93d9fa362` (Git blobs verified: A `b85bbb014a10eee7aab5e165d8772f68672a84bf`, B `bc618f99884aa55e7eff3fda9c232db2112c596f`, C `0ec85328b1eb740f9fae8769e8cc4754b9f50c87`). The actual original B01-31 raster [Actions artifact #12723984143](https://github.com/C8LyNn/RoboRougelike/actions/runs/40485097384/artifacts/12723984143) is input, not a simulated model. B01-31's subsequent W26 revision **already fixes color-crop framing** and generates `*-silhouette-64px/128px.png`; **DO NOT count our full-page-vs-crop exercise as a new producer bug or rewrite its source.**

## New independent W26 work, not duplicated B01-31

Executable `silhouette_stress.py` uses exact B01-31 **current** source crop `(142,264,620,529)` on the 700×866 real PNG files. It:
- Produces comparable **64px and 128px color AND neutral grayscale** images with unchanged source scene framing, not per-variant idealized zoom.
- Applies **four separate 25%-of-canvas occlusions** (left/right/top/bottom) to grayscale at each resolution; reports worst side by surviving pixels of bright painted material. This is a deterministic UI-obstruction **stress fixture, not perceptual recognition**.
- Writes a 3-column comparison and byte-inspectable `receipt.json`, including source SHA256, crop, 64/128 stats, four-side occlusion ratios, NOT_RUN markers. Sources remain unedited.
- `test_silhouette_stress.py` tests **20 actual-image and adversarial cases** (three immutable PNG digests, exact new source crop/HEAD, 12 normal images, 24 obstructed images, source tamper negative, reproducibility, grayscale channel equality, unsupported dimensions and edge direction, NEVER fake Unity/user approval).

**Measured diagnostic, real PNG input, current crop, 64px grayscale bright pigment (threshold max RGB >96, background/floor lines mostly excluded):**
 
| Option | 64px retained bright pixels before cover | Worst 25% edge | Bright pixels remaining ratio (64px) | Worst 25% edge / ratio (128px) | Interpret conservatively |
|---|---:|---|---:|---|---|
| A WHEEL | 185 | left | 0.859459 | left / 0.860025 | Thin exposed struts are vulnerable; recognizable 4-wheel *hypothesis* |
| B TRACK | 316 | left | 0.876582 | left / 0.852459 | Broad track masses keep larger bright footprint; **not proof** of actual recognition |
| C OMNI | 208 | left | **0.788462** | left / **0.779601** | More vulnerable when quarter canvas hidden: independent floating rings suggest unconventional drive, actual omni/hover mechanics UNKNOWN |

Counts are **bright painted RGB pixels**, not segmented full silhouettes, actual occupied cells, area of model mesh, measured collider or user responses. Grid, callout stroke, paint and color differences can affect count; differences do **not** establish that B is easier for humans, just identify a reproducible stress hypothesis. In-game camera visibility, occlusion by other parts and actual UI content remain `NOT_RUN`.

## Falsifiable next tasks / owner routing

**B01-31:** use 25% edge visual stress and grayscale sheet for artwork `REVISE` only if HQ explicitly requests; do not auto-redesign.

**B01-11 / B01-12:** blind-label-free recognition of three variants at 128/64, compare grayscale, and 25% left-side occlusion. Test 5 people as proposed in producer report; there were **zero real participants** here. Record confusion matrix A/B/C/UNKNOWN and confidence before revealing captions, then distinguish self-reported hover vs omni. This report supplies actual image fixtures, **not** valid UX study findings.

**B01-00:** make explicit decision `SELECT_A / SELECT_B / SELECT_C / MIX / REVISE / PARK`. Only after `ART.A2` can B01-30 + B01-10 produce `TECH_ART_CARD` and `DESIGN_CARD`. No model production before separate G-DESIGN/MODEL_JOB; PARTSIZE/CHL still exclusive B01-23/20. `BLOCKED_BY: ART.A2 HQ_DECISION_PENDING`.

**Truth boundaries:** `PNG_SOURCE_QA=PASS` only if actual CI runs on exact SHA; `MANUAL_READABILITY=NOT_RUN`, `UNITY_NATIVE=NOT_RUN`, `COLLISION/CLEARANCE=UNKNOWN`, `MODEL_JOB=BLOCKED`, `G2_HOLD`, `NO_MERGE`.
