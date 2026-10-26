# B01-30 W15 — independent verification of REAL GLB sweep receipt

## Confirmed false-green on the previous W14 source

Original owner source [Draft #244 at `ca6e874643e0fc7579d21f4b0b9de1df3837b965`](https://github.com/C8LyNn/RoboRougelike/pull/244) contains `tools/mount_envelope_candidates.py`. On an authentic W14 inner package (exact GLBs whose SHA-256 matches the old manifest), the test replaced **only** M04's `qa/M04-024_SWEEP.json`: every clip's recorded Z minimum/maximum/size changed from approximately -0.78878/+0.78878/1.57756m to -0.5/+0.5/1.0m. The **GLB binary SHA field stayed correct**, and W14 `measured()` incorrectly accepted the replacement (local direct interpreter proof performed before W15 changes). This can result in a misleading preview-fit recommendation even though model geometry is unmodified. It is an **evidence integrity defect**, not a demonstrated host gameplay/Unity collision.

## Concrete W15 repair

- New executable `tools/verify_sweep_provenance.py` **recomputes all actual source GLB vertices and node transform animation positions at exactly 121 sampled times per clip**, using existing owner `swept_geometry_qa.py` and glTF2 binary parser `rework_legendary_glb.py`.
- Compares saved sweep report **entire clip sequence**, IDs, 4-gimbal simultaneous channels, start/end times, vertex counts, min/max, span, five intermediate snapshot positions, mesh counts, source digest, sample count and evidence disclaimer.
- Fails closed on the first exact mismatching field with `SWEEP_RECEIPT_FORGED <model> <field>`. Also rejects tampered GLBs relative to committed manifest SHA-256.
- New actual-package `tests/test_sweep_provenance.py` covers genuine M01/M04 GLBs and **forged exact-source-hash sweep receipt**, changed min/max only, shifted intermediate frame, false channel count/clip name/time, false vertex count/mesh count, added/missing clip, changed sample count/disclaimer, positive immutable evidence, no claimed mount authority, and full-package negative check.
- Updated package-owning Actions CI recomputes evidence **before** W13 footprint and W14 mount proposal. Fails the build on bad receipt. Ships `qa/REAL_GLB_SWEEP_PROVENANCE_W15.json` and all 3 executable helper scripts inside deterministic Codex ZIP so receiver can rerun.
- Preserves model binaries, typed footprint, mass=null and non-authoritative visual empties; **does not install in Codex game**.

## Acceptance and evidence boundaries

Genuine binary GLB SHA-256: M01 `795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd`; M04 `c40ce2c090b232833340e3135d75184d01eb7f81c072ab07c80d46508f2af0d8`. W13 real depths still exceed proposed typed Z by **2.572m** and **0.47756m**; W14 **3×2×8 / 4×2×4** are only unapproved root-anchored geometric candidates. This verifier does **not** provide a second independent mesh-sampling algorithm, a proof between discrete sample times, Blender animation import, triangle-level narrow-phase, Unity PhysX, BUILD socket authorization or any native 720p/1080p image. These all remain `NOT_RUN`.

**Receiver:** B01-40 repeats independent G0 against immutable updated W15 PR head and inner ZIP SHA; B01-60 tests imported scene and raw 720/1080 + collider contacts **only when licensed Unity present**; B01-00/B01-10 chooses actual footprint/root policy, not producer visual QA. **NO MERGE / G2 HOLD**.
