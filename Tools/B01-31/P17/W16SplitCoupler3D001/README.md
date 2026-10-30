# B01-31 / PASS17 WAVE16 — ORIGINAL SPLIT COUPLER 3D CONCEPT

**Actual distinct source asset:** `B01-31-SPLIT-COUPLER-CONCEPT3D-001 / P17-W16-R1`.

Unlike W14 [Cable Snap VFX #557](https://github.com/C8LyNn/RoboRougelike/pull/557) (36 2D sprites) and W15 [four Unity .anim source clips #576](https://github.com/C8LyNn/RoboRougelike/pull/576), this original package produces a **real 3D low-poly split electric coupler**: two independently pivotable physical model roots, exposed copper pins, insulating rubber strain relief and PBR metallic/rough materials. It's a genuine **new art asset**, not another 2D sprite sheet, frame-reading test or a Save/Load subsystem.

## Physical geometry and original deliverables

**Units: 1 model coordinate = 1 meter; Y up.** Two separate original glTF nodes `LeftConnectorPivot` and `RightConnectorPivot` at x=−0.20/+0.20 m, each with its own geometry mesh and **six real PBR material primitives**: graphite housing, rubber cable, hazard amber collar, four copper contacts per side, cyan insulation stripes, metallic strain-relief collar. Both entire halves and their conductors remain **at least 0.26 m separated in the real vertex geometry**. Do not read the graphic as an in-game successful/allowed dock state. Color/materials are visual only. Model is authored algorithmically with actual closed cylinder triangle surfaces, explicit per-face normals, material PBR baseColor/metallic/roughness, one actual binary buffer per GLB and valid glTF 2.0 JSON/BIN chunks.

The generator physically exports:
- **`B0131SplitCouplerConcept.glb`**, actual GLB2 real triangle meshes, two independently transformable root nodes, PBR per-primitive materials.
- **`B0131SplitCouplerConcept.obj` + `.mtl`**, matching original geometry in OBJ/MTL interchange (OBJ bakes the two original pivot translations into its individual `o LeftConnectorPivot`/`RightConnectorPivot` group coordinates; GLB2 preserves actual two-node transforms).
- **Two real 1800×700 RGB PNG** three-view preview boards (isometric, side profile, reverse perspective) for dark/light color evaluation, produced by projecting/shading the actual authored source triangles — not fictitious Unity screenshots.
- **`receipt.json`** with actual per-binary SHA256, two node transforms, triangle counts, expected gap, real per-PNG checksums and truthful runtime/not-run gates.
- **Physical reproducible six-member ZIP**, produced by reading the true loose model/boards/receipt source bytes. This ZIP is **not a preverified Unity importer** or authorized game install.

All graphic geometry is original; no third-party/stock assets or existing source models edited. The **separate glTF node pivots are conceptual transform anchors, not authoritative part/socket UUIDs, colliders, mass, or persisted IDs**. Some original overlapping material sub-shell cylinders are deliberate for a stylized modular assembly; the individual cylinder surfaces are non-degenerate and original receipt verifies real triangles. There is **no Unity prefab, material .mat, physics collider, actual mechanics or model/job integration**. The OBJ files may be assessed for later Unity ModelImporter intake only by explicitly authorized owner.

## Reproduce and independent physical binary verification

With Python 3.11 / Pillow 11.3.0:

```bash
python -m pip install --disable-pip-version-check "Pillow==11.3.0"
python Tools/B01-31/P17/W16SplitCoupler3D001/build_model.py --output build/B0131W16
python Tools/B01-31/P17/W16SplitCoupler3D001/verify_model.py --source build/B0131W16
```

Independent `verify_model.py` actually decodes the GLB2 12-byte header, JSON and BIN chunks, all 36 real bufferViews and float32 VEC3 position/normal + uint16 index accessors. It audits **every actual face** for positive triangle area, correct normals, finite floats and bounds; verifies **two actual independently movable root positions, actual at least 0.26m body/pin gap**, OBJ/MTL vertex+face parity, authentic 1800×700 preview raster bytes, and exact real ZIP 6-member CRC. Four negative tampered-GLB controls must fail, including bad magic, length and JSON chunk. Dedicated CI executes original geometry production twice and **every actual model/preview/receipt/ZIP byte must be identical**, then **Windows2022 independent read-only receiver downloads actual Ubuntu output** and revalidates physical GLB/OBJ/MTL/PNGs without calling the generator.

## Governance, Unity, integration and rollback

[Wave16 #589](https://github.com/C8LyNn/RoboRougelike/issues/589) freezes [only Secure V2 #510@d94400d95d13db7f27e1533df05c9292d5380431](https://github.com/C8LyNn/RoboRougelike/pull/510) and [only Save contract #556@33094f154c7b91119b36d58aab285e6e83c5de57](https://github.com/C8LyNn/RoboRougelike/pull/556). B01-23/62 own adapter choice between #585 and #587 and P0 regressions #572/#582/#588; B01-61/40 own exact-source Save QA and rollback; B01-60 owns native. This 3D concept is **not** an adapter, Save/Load or transaction state; no game/Codex/Unity assets are touched and neither #510 nor #556 are modified.

**Gates:** original 3D physical source QA G0 only after actual CI; B01-40/30 independent visual mesh intake PENDING, B01-60 actual activated Unity6000.3.14f1/PhysX G1 NOT_RUN, CODEX_ACK_NO/INTEGRATION_NO, G2 HOLD. GLB/OBJ validity is NOT Unity native import proof. A separate authorized B01-30/40 receiver may assess actual meshes/material formats; B01-60 may use a disposable licensed Unity host to import OBJ and capture Editor.log/native behavior. Never fabricate Unity or Blender runtime tests.

**Ownership / rollback:** Only new `Tools/B01-31/P17/W16SplitCoupler3D001/{build_model.py,verify_model.py,manifest.json,README.md}` and unique `.github/workflows/b01-31-p17w16-split-coupler3d.yml` in PR. Revert only those own five files; any future disposable asset import may delete ONLY files specifically imported from this named W16 model, never foreign empty folders, W14/W15 originals, selected P0 code or Codex. No merge.

**Receiver:** B01-30/40 actual physical mesh/Unity asset intake and original geometry receipt; B01-60 licensed native model import/visual collision inspection if authorized; B01-12 optional visual ideation only; B01-00 may route #79 after independent gates.
