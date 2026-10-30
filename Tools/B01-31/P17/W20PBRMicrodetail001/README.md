# B01-31 — PASS17 W20 — Original industrial PBR microdetail: normal + AO/roughness/metallic

**Package:** `B01-31-COUPLER-PBR-MICRODETAIL-NORMORM-001/P17-W20-R1`. [W20 #668](https://github.com/C8LyNn/RoboRougelike/issues/668), governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## One genuine NEW art capability

Existing [W19 PR #653](https://github.com/C8LyNn/RoboRougelike/pull/653) has 6 **actual original hand/procedurally painted 256px RGBA base-color PNGs**, UV coordinates on all 12 original material primitives, and a binary GLB2 with 1312 genuine original coupler triangles. It does **not** contain any `normalTexture`, `occlusionTexture` or `metallicRoughnessTexture`. W20 supplies **actually new microdetail lighting materials**, rather than duplicating earlier base-color/3D motion/fragment effects.

`B0131SplitCouplerMicrodetailPBR.glb` preserves **the complete original W19 glTF BIN geometry, UVs, all six baseColor PNG images, the 54 old bufferViews, all 48 old accessors, original transforms/mesh primitives and all original material PBR factors byte-for-byte/semantically**. Only **twelve new physically authored image/png source buffers** and standard glTF2 material bindings are added. Six **original 256² tangent-space normal RGB PNGs** (per original UV material) produce actual depth/abrasion normal response. Six **original 256² RGB packed AO-roughness-metallic maps** contain **R=ambient occlusion, G=per-pixel roughness, B=per-pixel metallic**, linked **simultaneously** by `occlusionTexture` and `pbrMetallicRoughness.metallicRoughnessTexture`; all six original baseColor factors and metallicFactor/roughnessFactor multipliers remain intact.

Material identities remain: graphite worn shell, rubber cabling, hazard amber enamel, exposed scored copper, cyan insulation ID and metallic strain collars. These are **stylized original art normal/roughness approximations**, not measured PBR. The original texture paint and actual normal vectors are nonconstant and independently checked, not uniform placeholder maps. GLB includes **18 genuine embedded material PNGs** total (old 6 basecolor + 12 new), plus **twelve physically exported loose RGB 256px PNGs**, **two actual RGB 1800×740 material map boards** (normal and ORM, all six source material swatches per board), machine SHA256 `receipt.json`, and real reproducible **16-member physical source ZIP** (1 GLB + 12 PNG + 2 boards + 1 receipt).

**UV / tangent caveat:** W19 had original UV but no explicit `TANGENT` source vertex attribute. Standard glTF normal mapping requires the importer/renderer to build a valid tangent basis from UV and original normals. This package is a source-standard glTF2 material asset **NOT a native Unity proof**; Unity may need an approved glTF importer/converter and per-material URP shader mapping. The host must verify actual tangent generation and R/G/B channel parity. **No original gameplay or Save/Load code, prefab, MonoBehaviour, collider, #510, #556 or Codex source modified.**

## Verify actual original W19 model and original W20 generated PBR assets

Use immutable original [W19 completed Actions run #41110977749](https://github.com/C8LyNn/RoboRougelike/actions/runs/41110977749) / [actual physical artifact #13003621811](https://github.com/C8LyNn/RoboRougelike/actions/runs/41110977749/artifacts/13003621811). The input original `B0131SplitCouplerWornLivery.glb` and its `receipt.json` **must** physically exist together in `./original-w19/B0131W19`. Original immutable GLB SHA256 **`f8683e5ee3f0916fbde534895e5e01f890bf577b0083858e8dc361b1f8d5ae41`**.

```bash
python -m pip install --disable-pip-version-check "Pillow==11.3.0"
python Tools/B01-31/P17/W20PBRMicrodetail001/build_materials.py \
  --original original-w19/B0131W19 --output build/B0131W20
python Tools/B01-31/P17/W20PBRMicrodetail001/verify_materials.py \
  --original original-w19/B0131W19 --output build/B0131W20
```

`verify_materials.py` **does not import generator**. It actually decodes original W19 physical GLB2 SHA and geometry and new W20 physical GLB JSON/BIN, asserts complete original source BIN byte-prefix and original mesh/UV/PNG PBR factors untouched, all **18 physically embedded RGBA/RGB PNG sources** and standard material bindings, all 12 new source PNG pixel data and normal vector lengths/forward blue channel, ORM R/G/B ranges and nonconstant material details. It opens **both physical six-tile 1800x740 atlas images**, checks **16 exact deterministic ZIP members and CRC/byte parity**, and intentionally corrupts six semantics + two GLB headers (all **8 must RED**). Ubuntu original source full output built **twice**; every GLB/PNG/receipt/ZIP SHA must match exactly. **Independent Windows2022** downloads true Ubuntu art and true immutable W19 source, checks physical GLB and raster bytes without invoking creator.

## Receiver, formal gates and non-destructive rollback

- **B01-30 / B01-40** — independent formal G0 for real original W20 GLB2, embedded original PNG/UV source identity, PBR/mapping and approved disposable scratch, safe rollback including foreign empty dirs, symlinks and dirty original files.
- **B01-60** — native actual Unity6000.3.14f1 via authorized safe local Windows runner [#667](https://github.com/C8LyNn/RoboRougelike/issues/667) if operator executes. Check actual importer, tangent generation, URP/shader normal and metallic roughness, Editor.log, exact test case IDs, activated license. Until evidence, `NATIVE_G1_NOT_RUN`, **NOT** engine renderer PASS.
- **B01-12 / B01-00** — potential non-authoritative cosmetic variant only after formal G0/G1 approval and deferred Codex availability.

W20 frozen gameplay #510, frozen wire Save #556, selected R2 #596 and previously selected R3 #642 remain untouched. New owner-selected R4 #647 vs #652 belongs exclusively B01-23/62; canonical Save/Load API [#666](https://github.com/C8LyNn/RoboRougelike/issues/666) and Windows G1 runner #667 belong their assigned owners. This art PR does **not** resolve durable Commit ACK-loss, ID/rollback or Save/Load atomicity; none of those results can be inferred from a PBR texture QA.

**Gates** `IMPLEMENTED=YES_SOURCE`, `TESTED=ONLY_REAL_CI_ON_EXACT_SHA`, `QA_ACCEPTED=NO`, `READY_FOR_INTEGRATOR=NO`, `INTEGRATED=NO`, `RELEASED=NO`, `SOURCE_G0=PENDING`, `G1=NOT_RUN`, `G2=HOLD`, `CODEX_ACK=NO`. No merge. **Rollback** = remove exactly five NEW W20 owner source/workflow paths; if separately authorized disposable target installs W20 source, delete only distinctly W20-named imported model/textures and generated scratch data, NEVER W19 origin, W18/W17/W16, foreign empty dirs, other owners' files or frozen game code.
