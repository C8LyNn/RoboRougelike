# B01-31 — W26 dependency-reactive risk assessment for CM001 A/B/C

**Task:** `B01-31-W26-CONCEPT-MOBILITY-001-VISUAL-REVIEW`  
**Input concept SHA:** `62b9b6660819ff71ad007843802ac0e93d9fa362` (original W17 SVG A/B/C, **not** revised model geometry).  
**Producer context at inspection:** PARTSIZE [PR #286](https://github.com/C8LyNn/RoboRougelike/pull/286) `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`; CHL [PR #285](https://github.com/C8LyNn/RoboRougelike/pull/285) `077e692785aff2990a2d72a80fd1db62ac57b0e8` (not an art dependency).  
**B01-30 source evidence:** [PR #281](https://github.com/C8LyNn/RoboRougelike/pull/281) at `493a9054fc0bc15417cdb697c7ee266faa82d207`: original R3 Wheel/Track/HeavyGun/Suspension static analytical physical/visual envelope study. Its numbers are for **existing R3 meshes**; do not assign their bounds to these NEW 2D concept silhouettes.

## Key W26 correction: fair silhouette thumbnails

The original CI `qa_render.py` reduced entire full-page design **cards** to 64/128px, compressing text, disclaimer, reference cube and art into one tiny image. It proved that PNG bytes existed, **not** that mobility silhouettes were visible. The W26 revision now also crops **only** the same illustration area `(142,264)-(620,529)` from each 700x866 individual card, preserving identical framing, aspect ratio and interpolation. It produces six subject-only images `*-silhouette-64px.png` / `*-silhouette-128px.png` plus one `silhouette-review-128-64.png` comparison sheet. The original source SVG and full-card previews stay untouched. Both sets of thumbnails remain useful for different questions; do not mistake one for the other.

This change is **test preparation**, not measured camera legibility. A real reader must still blind-identify the three unlabeled silhouettes; no participants have been recruited.

## Independent concept-level risks, in rank order

| Rank | Concept | Visual/functional interpretation | Main risks inferred from illustrations, NOT measured physics | Falsifiable next check |
| --- | --- | --- | --- | --- |
| **P0** | **C (CM001-C-OMNI)** | Ring pods on four arms might communicate *hovering*, but could also look like glowing wheels or four free-floating bearings. | (1) Unsupported implied non-contact motion; actual movement policy does not exist as approved feature. (2) Floating-ring arms would require explicit signed clearance and trustworthy collision/attachment policy, not 1×1 virtual cube. | Blind 64px classification: did observers distinguish unconventional omni from four-wheeled? In future G-DESIGN, choose wheeled-omni **or** true-hover explicitly, then physically measure contact/swept pod envelope. |
| **P0** | **B (CM001-B-TRACK)** | Side-long closed belts clearly imply crawler tracks at medium size. | (1) The drawn front and rear track arcs extend beyond a symbolic cell; assuming one exposed grid face suffices for mounting would be a false physical authority. (2) Belt contact, steering, track belt animation and real trench/bridge clearance are unmeasured. | Source-native (post G-DESIGN only): 0/90/180/270 yaw measured collider sweep + contact/no-snag around a neighbor; user recognition and visual occlusion at 64px. |
| **P1** | **A (CM001-A-WHEEL)** | Four tires and visible arms are familiar modular mobility language. | (1) Open arms may imply playable suspension/travel whose movement has not been modelled; do not promise MOV009 behavior. (2) Wheel diameter/struts obscure side surface; conceptual placement may appear valid yet physical sweep or typed socket not verified. | Annotate where five viewers *expect* a mount, and compare against later authoritative sockets/4 yaw when B01-23 has them; at 64px ask wheel vs suspension vs body. |

### Source-specific physical caveats

- Real R3 visuals surveyed by B01-30 include Wheel **0.52 × 0.91 × 0.78m**, Track **0.52 × 0.676 × 0.936m**, Hover with Y overhang ~0.0422m. These values **cannot** be transferred onto CM001 drawings: the concepts have no CAD-consistent orthographic geometry, no mesh vertices, collider or rig.
- The 0.55m cube in each art board indicates **grid reference**, *not* a measured occupied cell count, pivot/CoM/mass, actual socket face, or collision volume.
- The latest PARTSIZE broadphase/source updates may reduce false-green mounting from omitted physics overlap and separate typed socket authority, but **native PhysX and authenticated socket behavior remain NOT_RUN**; visual design cannot claim `READY`.
- For B, rotating a long track changes swept world envelope; for C, rotating ring pods could change both signed occupancy and hazard/clearance; for A, wheel vertical suspension travel exceeds the rest state. All three remain **UNKNOWN** until separately measured.
- Risks above are *hypotheses*, **not** source bugs assigned to B01-23; do not open production tickets on the basis of concept art.

## Three explicitly separate views for HQ

1. **Full card**: conceptual intent, shape, annotated functional hypothesis, two risks each.
2. **Unlabelled silhouette-only thumbnail** at **128 and 64 px**: visual recognition task; labels should be hidden from participants before identification to avoid priming.
3. **Actual PARTSIZE source-driven outline** only *after* G-CONCEPT + G-DESIGN: authoritative measured sizes/socket/clearance. **Unavailable now.**

## Minimal blind-visibility test plan (NOT_RUN, proposed)

| ID | Task / fixture | Proposed acceptance threshold | Interpretation of failure |
| --- | --- | --- | --- |
| VIS-01 | 5 naïve participants see A/B/C unlabeled at 128px, shuffled; ask movement method | A,B ≥4/5 recognitions; C ≥3/5 marked “unconventional/uncertain” rather than claiming actual flight | REVISE silhouette; no gameplay assumption |
| VIS-02 | Same 5 at 64px; ask wheel vs track vs experimental | A,B ≥4/5, C ≥3/5 | REVISE lower-detail value shapes |
| VIS-03 | Neutral grayscale/low-saturation on dark UI; identify three options | A,B ≥4/5 | Increase silhouette contrast, not just emissive color |
| VIS-04 | Partial occlusion: overlay nominal opaque garage UI on 25% of subject edge, then identify | ≥3/5 for A,B; record C separately | Avoid burying mobility cue under modular body/UI |
| VIS-05 | Point to what participant thinks is the mounting point on full card | Record all answers; **zero mechanical PASS inferred** | Add “attachment UNKNOWN” annotation; do not create socket without verified tech art |

**No participant studies performed** — all thresholds are DESIGN_PROPOSAL and all UX results `NOT_RUN`. Even if source pixels are distinct and SVG/PNG CI PASS, such automated checks do not prove perception.

## Decision gate / next actionable branch

`ART.A1 DONE (SVG+PNG)` → **`ART.A2 BLOCKED_BY HQ DECISION`**. Request one explicit outcome:
`SELECT A / SELECT B / SELECT C / MIX A+B / REVISE / PARKED`.
An implicit absent response is *not* SELECT. For SELECT/MIX, next B01-31 work: **only** chosen option orthographic visual sheet with UNKNOWN geometry disclaimers; then B01-10 game design + B01-30 technical art, HQ G-DESIGN, separate MODEL_JOB (if capacity and acceptance exist). Until then no Model Lab, no Blender/GLB, no host Unity and no G2.

**Ready now for B01-11/B01-12:** subject-only raster preview and blind recognition script, independent of native Unity. No request to mutate their existing UX prototype until they choose to consume the artefact.
