# B01-31 — PASS 17 / WAVE 19 — Original Coupler UV Wear Livery

**Package:** `B01-31-COUPLER-UV-WEAR-LIVERY-001/P17-W19-R1`. Sole role B01-31 — concept art and visual exploration. [HQ W19 #646](https://github.com/C8LyNn/RoboRougelike/issues/646).

## Actually distinct new visual function

Existing [W16 original split electric coupler #599](https://github.com/C8LyNn/RoboRougelike/pull/599) is a **real physically authored 3D GLB2 model with two independently movable roots, 1312 triangles and six untextured PBR materials**. [W17 #614](https://github.com/C8LyNn/RoboRougelike/pull/614) adds root movement and [W18 #636](https://github.com/C8LyNn/RoboRougelike/pull/636) adds genuinely separate 3D fragment VFX. Neither adds UV maps or real texture images to W16 body. W19 adds **real UV coordinates and industrially weathered six-material base-color image textures** to the **immutable original W16 3D geometry**; it is not another shard or VFX animation.

The pipeline reads exact immutable original [W16 successful physical Actions run #41097210391](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097210391), [actual source artifact #12996617273](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097210391/artifacts/12996617273), original GLB SHA256 **`eeae6a553ca6136b5eaa97a65d5dd995f5f28fcb8bc54eb2180dfe28bc3653d7`** and original 1312-triangle receipt. Does not regenerate or mutate that original geometry.

Produces one real **`B0131SplitCouplerWornLivery.glb`** (glTF 2.0 binary): source original complete GLB geometry BIN prefix preserved **byte-for-byte** plus all original 36 accessors, 36 original buffer views, both original transforms, all 12 original material primitives and all original PBR parameters except additive actual `baseColorTexture`. Original six actually painted **256×256 RGBA PNG** textures are physically embedded *inside the GLB* as six real `image/png` bufferViews with six distinct `textures[]` entries and material references. All original **12 material primitives** gain a genuine physical FLOAT32 `TEXCOORD_0` VEC2 buffer at full original vertex count, derived from actual original geometric X and circumferential angle. PBR renders as existing original material factor multiplied by actual painted base-color texels.

Six original art surface directions: **scuffed graphite shell, ribbed rubber cable, industrial diagonal hazard bands, copper wear scoring, cyan electrical identifier rings, worn metallic strain-relief collar**. The generated imagery is not a colored placeholder: original hazard stripes, UV-space panel markings, rivet fields and deterministic abrasion micro-scratches are painted as actual pixels.

Deliverables: actual textured GLB; six standalone original 256x256 RGBA PNG texture files (also embedded byte-for-byte in the GLB); two real 1800×720 RGB dark/light three-view concept boards **projected and colored using actual original 1312 triangles and actual embedded UV texture texels**; true machine per-asset `receipt.json`, and a reproducible 10-member physical archive. The review images are NOT Unity Editor screenshots, native model importer evidence or gameplay.

## Reproduce actual art files and physically verify all source bytes

Extract original immutable W16 artifact such that `B0131SplitCouplerConcept.glb` and W16 `receipt.json` are in `./w16-original/B0131W16/`. No source GLB modification is permitted; any wrong original source digest or receipt leads to fail-closed.

```bash
python -m pip install --disable-pip-version-check "Pillow==11.3.0"
python Tools/B01-31/P17/W19CouplerUVLivery001/build_livery.py --original ./w16-original/B0131W16 --output ./build/B0131W19
python Tools/B01-31/P17/W19CouplerUVLivery001/verify_livery.py --original ./w16-original/B0131W16 --output ./build/B0131W19
```

The separate verifier **does NOT import the producer**. It opens both **actual frozen original W16 GLB** and the new physically written textured GLB, checks all original source mesh/normal/index bytes unchanged (including the exact original BIN prefix), 36 old bufferViews/accessors unchanged, all 12 new UV accessors and genuine original 3D vertex-to-UV mapping, all six actual physically embedded PNG bytes exactly matching the six standalone PNG files, six actual material references, both dark/light three-view raster images, physical ZIP members+CRC, and **eight corrupt/semantic negative mutations explicitly denied**. Full original output (GLB/6 PNG/2 boards/receipt/ZIP) **rerenders twice**, SHA parity of *every* physical file must match. Separate Windows2022 receiver downloads the actual Ubuntu-built W19 art binary and original frozen W16 artifact and independently re-decodes it WITHOUT running authoring script.

## Separate governance / receiver / rollback gates

W19 [#646](https://github.com/C8LyNn/RoboRougelike/issues/646) retains **immutable frozen** [#510@`d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510), [#556@`33094f154c7b91119b36d58aab285e6e83c5de57`](https://github.com/C8LyNn/RoboRougelike/pull/556), [#596@`efb2b06eafedab5469b57f163152d96aca3c5915`](https://github.com/C8LyNn/RoboRougelike/pull/596). #604 stays **ALT_HOLD**. Only B01-23/B01-62 may select R3 #642 vs #644; B01-31 makes no such selection and cannot certify host security, stale-world rollback or game ID persistence. **No change to canonical Unity `Assets/**`, no Codex code, no merge.**

B01-30/40: formal independent 3D GLB+UV+textures source intake in an isolated disposable workspace with safe non-destructive rollback, inspect asset no GUID collisions; B01-60: only actual legally activated Unity6000.3.14f1 native importer+shader/URP/render QA counts as G1, and **Unity does not automatically guarantee glTF importer support**, external approved pipeline may be required. B01-12 may offer visual readability integration after gameplay owner approval. B01-00 may route to Codex Inbox #79 only after formal gates.

**Status:** source-only `IMPLEMENTED` after commit; `TESTED` only after executed original CI; formal `QA_ACCEPTED=NO`, `READY_FOR_INTEGRATOR=NO`, `INTEGRATED=NO`, `RELEASED=NO`. B01-30/40 formal G0 PENDING, B01-60 G1 NOT_RUN, G2 HOLD, Codex ACK NO, no merge. Original source binary tests do NOT demonstrate native Unity gameplay/SafeLoad/PhysX or completed Codex intake.

**Rollback:** delete ONLY five W19 newly added source/workflow paths, and in any *authorized disposable* 3D import target only exactly named W19 GLB/textures; never delete original W16 model, older W17/W18 assets, foreign empty directories, canonical #510/#556/#596, #604 or Codex.
