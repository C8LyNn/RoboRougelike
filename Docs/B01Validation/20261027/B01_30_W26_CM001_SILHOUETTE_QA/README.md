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

## PASS 3 — technical interpretation for HQ: not yet a TECH_ART_CARD

**Current exact source pins checked:** B01-31 [#294](https://github.com/C8LyNn/RoboRougelike/pull/294) `3efd76bb2eaa319282a51ad588f8f4176d0cc33b` unchanged original 2D A/B/C SVG, B01-30 visual stress [#299](https://github.com/C8LyNn/RoboRougelike/pull/299) earlier source at `2e8370b51cb4e3d124812dd7ae7e4b11c1ec6501`, B01-23 actual PARTSIZE [#286](https://github.com/C8LyNn/RoboRougelike/pull/286) `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`. The image-coordinate markers in `tech_art_hypotheses.py` come from visual reading of real unchanged 2D art; those markers **ARE NOT vertices, a 3D pivot transform, a socket, an authoritative footprint or measured dimensions**.

### New high-impact discovery: these are illustrations of *assembled VEHICLES*, not a proven single attachable part

Each drawing A/B/C contains a **central platform/body together with several distributed mobility elements**. The unit of authoring is undecided. Turning the whole art image directly into `1 MODULE` would risk hiding actual BUILD requirements (local ownership, independent breakage, actual wheel contact, typed mount surface, correct physics, animated swept envelopes). A '0.55m cube' caption is only a drawing comparator and cannot license these concepts into `1x1x1`, `2x2x2` or any other positive-grid `PartSizeDefinition`.

| Concept | Real 2D evidence + previous 64px stress | Hypothetical authoring breakdown (no asset yet) | Candidate **pivot/axis roles** — NONE measured | Potential mount/clearance gates — ALL UNKNOWN |
|---|---|---|---|---|
| **A — four-wheel** | Four isolated visible disc hubs, diagonal struts; grayscale 64px brightest 185px, 25%-left retains 85.95% (diagnostic only) | One **illustrated frame**, four wheel hub/rim assemblies, four struts/wishbones, steering carrier if chosen | Wheel-spin pivots **at each hub CENTER**; wheel spin **normal to each disc plane**; steering kingpin **up from carrier?**; suspension swing around each frame-arm **joint?**. All questions, no confirmed XYZ or mechanical hierarchy. | Frame-side typed sockets, axle/bearing location, wheel-radius swept cone, load/ground contact, vertical travel; negative grid offsets from real existing R3 wheel are **NOT transferrable** |
| **B — twin tracked** | Long, filled continuous track loop each side, three inner idler-looking circles per belt; grayscale 64px brightest 316px and retains 87.66% worst quarter | One **illustrated frame**, two independent track modules; each track may require idlers/sprockets and moving belt mesh or UV scroll | Sprocket/idler spin **through each drawn circular hub**; belt path follows loop; **no single pivot makes the full track belt spin as a solid rigid object**. Actual track articulation, drive transfer and wheel-pivot directions UNKNOWN. | Continuous belt ground contact vs visual-only belt, axle placements, hull-side typed hardpoints, side overhang/turn/snag sweeps, correct physics collider (not illustrative full silhouette) |
| **C — quad ring pods** | Four disconnected ring-like illuminated pods via arms; grayscale 64px brightest 208px but only **78.85%** retained under 25% left occlusion | One **illustrated hub frame**, four arm/strut pieces, four independently posed rings/pods, optional emissive-only trim | Arm-root swivel or pod local gimbal **possible**, not proved; independent pod yaw/roll and emitter animation **design questions**. Ring oscillation/glow does not demonstrate lift force. | **Hard design branch first:** real hover/non-contact or omni-wheel/contact? Four-arm swept clearance, underside thrust/ground-contact envelope, mounts at hub-to-arm and arm-to-pod, camera legibility/sound |

**Critical semantic distinction:** A visually defined *hub location* on an image is not a validated `PartSizeNativePhysics.ValidateMountSocketAuthority` typed socket. Even if the PARTSIZE source has `Physics.OverlapBox` query or `IsExposedFace`, a decorated frame face is not necessarily authorized for attachment. No port locations, parent-relative mesh units, local axes or signed-cell offsets can be inferred from the one 3/4 illustration. Use `UNKNOWN_NOT_MEASURED`, not fabricated coordinates. Existing #281 measured old R3 Wheel/CrawlerTrack/HeavyGun geometry only, **not new CM001**.

### Concrete preproduction hierarchy questions to unblock a chosen option

- **A:** Does HQ mean a complete four-wheel *chassis visual configuration* or a reusable **one-wheel kit**? If one wheel, separate single-wheel socket and steering drive contract. If full chassis, who owns all four independent collisions and contact patches? Steering-only vs suspension/travel vs fixed-wheel is an independent gameplay decision for B01-10.
- **B:** Is it **one symmetric pair of tracks as a compound propulsion module**, or two swappable side-track modules? Is belt geometry truly animated/skinned, driven by idlers, or a static hull plus UV scroll? Dynamic physical belt collision is not interchangeable with visual motion, and the drawing does not define the mechanical track-contact model.
- **C:** Are the four rings **hover thrusters** (no ground contact) or **omnidirectional contact wheels** (contact + traction)? Their identical art can communicate either; HQ should choose before any tech-art pivot/animation graph. Are arms moving, detachable, damageable or solely silhouette? A non-contact hover policy is not currently proven by this art.

**Recommended minimum HQ decision sequence:** FIRST (1) select **A/B/C/MIX/REVISE/PARK**, (2) decide authoring unit `SINGLE_ATTACHABLE_PART | MULTI_PART_MODULE | WHOLE_VEHICLE_CONCEPT`, (3) select motion meaning A fixed/steered/suspended; B belt purely visual vs mechanically simulated; C hover vs contact omni. Only after that B01-10 may produce design parameters, B01-23 provide typed socket/physical envelope consultation, and B01-30 finalize an actual `TECH_ART_CARD`. **No selected variant or MODEL_JOB was found during this pass, so this section is a decision aid, not stage ART.A3 passed.**

### PASS 3 late source correction — old pixel stress superseded for blind user tests

**B01-31 PR #294 advanced to `7a1416d3e10ee720d9d28931a3c268f95e2bba15`.** Its sole change since `3efd76bb...` was `qa_render.py`: new blind silhouettes remove the 0.55m reference-cube label from an in-memory copy of each source SVG, leaving the 3 authored SVG visuals byte-for-byte unchanged. The original technical full-card artwork intentionally RETAINS its grid comparator. Producer real [CI #40518648031](https://github.com/C8LyNn/RoboRougelike/actions/runs/40518648031) passed source visual parser and pixel checks 36/36; [authentic artifact #12739939008](https://github.com/C8LyNn/RoboRougelike/actions/runs/40518648031/artifacts/12739939008) contains the six updated 64/128 PNGs.

The **earlier 185/316/208 bright-pixel counts and 85.95/87.66/78.85%** are from previous full-card-source crop with reference overlay and different content margins. **HISTORICAL, not a current blind-study acceptance.** The new `blind_reference_free_qa.py` independently pins the NEW six original reference-free PNG SHA256, inspects actual current 64px/128px original images, runs same grayscale bright-pigment >96 / 25%-four-edge stress, and outputs `current_producer_reference_free_stress.json`. Its 12 adverse/source checks refuse forged new rasters; old stress tool remains only for provenance and the visible annotated technical card, not current blind scores.

| Concept | NEW 64px source blind bright pixels | NEW 64px 25%-left retention | NEW 128px 25%-left retention |
|---|---:|---:|---:|
| A — Wheel | **142** | **87.32%** | **87.29%** |
| B — Track | **237** | **93.67%** | **88.64%** |
| C — Omni | **170** | **80.00%** | **79.87%** |

These are independent **source raster measurements**, not manual human interpretation. The new B01-31 clean PNG deliberately differs from the technical annotation art: full-card annotated view can show the 0.55m scale comparator; **blind user recognition samples must not show it**. The percent score measures only surviving bright pixels, not total silhouette mass or UX readability. Design hypothesis remains that C is most susceptible to losing visible cues under left-edge occlusion; no concept selected, no accepted physical pivots or actual 3D.

### New actually executable evidence

`tech_art_hypotheses.py` inputs immutable genuine full 700x866 source PNGs from B01-31 Actions artifact #12723984143, uses the **current producer** common subject crop `(142,264,620,529)`, and emits:
- `CM001_TECH_ART_PRESELECTION_PIVOTS_UNKNOWN.png`: 3 labelled real art panels. Four **screen-space locus markers** per option illustrate *questions* (frame, hub, track belt, pod); no world coordinates or claims of physical compatibility.
- `tech_art_receipt.json`: exact input SHA256, 3 breakdowns, 12 distinct **2D** markers, animation questions, socket not-authorized, explicit G-CONCEPT pending, human/Unity NOT_RUN.
- `test_tech_art_hypotheses.py`: test exact published PNG pins and visible overlay, all unknown sentinels, reject forged `APPROVED`, fake sockets or extra-world-space pivots, reject shifted/synthetic art. Uses only real B01-31 art, no M01–M05 source or Unity scene.

**Expected downstream process:** independent QA of these 2D assertions may pass; actual 3D pivot consistency between orthographic views, Blender hierarchy, glTF animation axes, PARTSIZE multi-cell occupancy, Renderer.bounds, Collider.bounds and real 6DOF sweeps remain **NOT_RUN/UNKNOWN**. No model is created or accepted by this pass.

## Falsifiable next tasks / owner routing

**B01-31:** use 25% edge visual stress and grayscale sheet for artwork `REVISE` only if HQ explicitly requests; do not auto-redesign.

**B01-11 / B01-12:** blind-label-free recognition of three variants at 128/64, compare grayscale, and 25% left-side occlusion. Test 5 people as proposed in producer report; there were **zero real participants** here. Record confusion matrix A/B/C/UNKNOWN and confidence before revealing captions, then distinguish self-reported hover vs omni. This report supplies actual image fixtures, **not** valid UX study findings.

**B01-00:** make explicit decision `SELECT_A / SELECT_B / SELECT_C / MIX / REVISE / PARK`. Only after `ART.A2` can B01-30 + B01-10 produce `TECH_ART_CARD` and `DESIGN_CARD`. No model production before separate G-DESIGN/MODEL_JOB; PARTSIZE/CHL still exclusive B01-23/20. `BLOCKED_BY: ART.A2 HQ_DECISION_PENDING`.

**Truth boundaries:** `PNG_SOURCE_QA=PASS` only if actual CI runs on exact SHA; `MANUAL_READABILITY=NOT_RUN`, `UNITY_NATIVE=NOT_RUN`, `COLLISION/CLEARANCE=UNKNOWN`, `MODEL_JOB=BLOCKED`, `G2_HOLD`, `NO_MERGE`.
