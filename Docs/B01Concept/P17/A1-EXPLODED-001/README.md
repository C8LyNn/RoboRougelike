# A1-EXPLODED-001 — detachable wheel + support-arm research assembly

**Owner:** B01-31 (Concept Art) · **Release:** P17-R1 · **Status:** ORIGINAL RESEARCH ART, NOT UNITY INTEGRATED

## What is new

`A1-exploded-assembly.svg` is an original **exploded visual breakdown** of the only HQ-approved A1 single detachable wheel + support arm. Four distinct, spatially separated vector pieces:
- **mount** — mounting face, explicitly `DESIGN_PROPOSAL_UNKNOWN`;
- **arm** — visible structural support, with no claim of joint length;
- **damper** — visual-only suspension indicator, no measured stroke;
- **wheel** — tire and hub silhouette, no axle/torque/collider proof.

The original approved [A1 source PR #294](https://github.com/C8LyNn/RoboRougelike/pull/294) Git blob `96cc46a7c14f94a45c387e102d5940cb787d2d5c` is included as read-only `A1-approved-reference.svg`, preserving its original vectors exactly. Source remains owned by B01-31; B/C are parked. Separate design/MODEL_JOB approvals are still required.

## What Codex / B01-30 can actually inspect

Open the new `A1-exploded-assembly.svg` in any SVG viewer, or view the **real output PNGs** from the P17 Actions artifact. The original vector layers remain semantically identifiable by `data-source-id`:
`part-mount-proposal`, `part-support-arm`, `part-damper-visual`, `part-wheel`.

The raster command generates a 1920×1080 complete illustrated sheet plus **four actual transparent sprites at 128×128 and 64×64** for visual inspection; sprite sizes are image dimensions, **not** physical PARTSIZE metrics.

## Reproduce / acceptance

Python 3.11+:
```sh
python -m pip install "CairoSVG==2.8.2" "Pillow==11.3.0"
python Docs/B01Concept/P17/A1-EXPLODED-001/render_and_verify.py
```

Expected **PASS**: original approved Git blob matches pinned source, 4 nested groups are vector-identical to original A1 groups, 4 independently transformed research components, no duplicate element IDs, actual CairoSVG sheet dimensions correct, nonblank pixels, actual transparent per-part sprites and nonempty PNG files. Output receipt `rendered/A1-P17-receipt.json` includes PASS/FAIL and exact SHA256 + CRC32 of all PNGs.

**Do not claim** human legibility, real in-game camera, mechanical fit, socket width, suspension travel, pivot, inertia, mass, collider, URP materials, damage effect, G1 or Unity runtime validity. These checks are NOT_RUN.

## Isolated import / rollback (Codex operator)

1. Fetch immutable Draft PR head commit and run the QA command in a **disposable checkout**. Verify its output evidence and the manifest SHA.
2. For **design/visual review only**, use `A1-exploded-assembly.svg` or generated PNGs. If the part later gets an authorized mechanical spec, independent B01-30/10/23 G-DESIGN and a **separate** Model Lab MODEL_JOB must precede any 3D Unity integration.
3. **Do not install** this art directly into `Assets/**`, alter the Codex garage, or claim that this 2D proposal is an attachable prefab. No G2 release or merge is requested.
4. To rollback/delete the standalone research packet, remove `Docs/B01Concept/P17/A1-EXPLODED-001/**` and `.github/workflows/b01-31-p17-exploded.yml` from the disposable checkout. No canonical game files were ever modified.

**Next owners:** B01-30 tech-art source silhouette receiver, B01-10 / B01-23 for actual attachment mechanics, B01-40 for handoff validation, B01-60 for independent research-source QA. Codex alone decides future integration after HQ gates.
