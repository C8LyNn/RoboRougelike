# B01-31 — P17/W23 — Three W22 Armor Models as Real Unity-Importer-Friendly OBJ/MTL

**Unique package** `B01-31-W23-ARMOR-OBJ-IMPORT-BRIDGE-001/P17-W23-R1`. [HQ W23 #731](https://github.com/C8LyNn/RoboRougelike/issues/731). Producer: B01-31 original visual-art/export pipeline. **One new useful function: zero-new-plugin OBJ model import path** for three real W22 mesh models previously only available as glTF2, which requires a separately approved importer in Unity. This is not a fourth variation of existing pbr paint or geometry.

## Original source provenance; no overwritten W22
Read ONLY the authentic immutable [W22 Draft #725@93ee04c56ea27ade2fa0b78b653acbe07790a370](https://github.com/C8LyNn/RoboRougelike/pull/725) physical 3-state GLB files from [source Actions #41129782579](https://github.com/C8LyNn/RoboRougelike/actions/runs/41129782579), [actual downloaded original artifact #13012642357](https://github.com/C8LyNn/RoboRougelike/actions/runs/41129782579/artifacts/13012642357). Original actual geometry and specific SHA:
- `OK` 204 indexed triangles — `a063ef3d8750ef7e65bd9f7ba57925e68f112d4f51e78f2048fe6552fb33fdfd`
- `DAMAGED` 264 indexed triangles — `9f710d0b88d7b6b4df1898c2c26b488987e89a83a382f0ad85df6af1ee16c8fe`
- `DESTROYED` 288 indexed triangles — `f7c994a3bd80dbf6c24655d06f26f13f95ba5e6d65b6590c5795dfc71243c20f`

The W23 exporter reads genuine GLB2 JSON/BIN actual FLOAT32 positions and normals plus UINT16 face indices, never regenerates base meshes or touches previous W22 PR. It emits **three physical original indexed OBJ files** (identical 756 actual triangle faces in total), with per-face `v//vn` indices and `usemtl` groups for all six original source material IDs. **FLOAT32 geometry and each corner normal round-trips bitwise exactly**; same coordinates and original pivot (0,0,0), no conversion, mirroring or remeshing. No UV is fabricated where source had none.

One real shared `B0131ArmorModule1x2x2-W23-shared.mtl` contains approximate traditional OBJ/MTL `Kd`/`Ks`/`Ns` from original six PBR materials. **OBJ MTL is not equivalent to glTF PBR**, so all original precise glTF PBR baseColor/metallicFactor/roughnessFactor values are preserved in `original-pbr-materials.json` for approved Unity URP material mapping, and Unity native shader appearance is NOT asserted. Two actually pixel-rendered **1800×625 RGB three-state comparison sheets** are generated from the *new OBJ* source at ISO and face cameras. `receipt.json` plus a deterministic **eight-member physical ZIP** carry the exact binary hashes and sizes.

## Reproduce exact original assets

Original W22 physical artifact must be extracted so `original-W22/B0131W22/` contains three untouched `B0131ArmorModule1x2x2-STATE.glb` and `receipt.json`. Any wrong original W22 GLB SHA or state receipt fails closed.

```bash
python -m pip install --disable-pip-version-check "Pillow==11.3.0"
python Tools/B01-31/P17/W23ArmorObjImportBridge001/build_obj.py --original original-W22/B0131W22 --output build/B0131W23
python Tools/B01-31/P17/W23ArmorObjImportBridge001/verify_obj.py --original original-W22/B0131W22 --output build/B0131W23
```

Independent verifier **does not import producer**. Re-decodes all original W22 GLBs and new physical OBJ, compares **every original float32 triangle position, vertex normal, material assignment** (not just counts), verifies exact source hashes and PBR sidecar, validates actual one relative `mtllib`, all six actual MTL materials, opens both 1800px RGB boards, verifies 8-member ZIP CRC and raw member parity, and rejects **eight deliberately corrupted OBJ or GLB inputs**. Ubuntu original creation+verification repeated twice and every SHA compared. Separate Windows2022 read-only receiver downloads actual Ubuntu-exported OBJ & W22 GLBs (no generation); independently checks exact source geometry parity.

## Governance, formal intake, native blocker and safe rollback

[#731](https://github.com/C8LyNn/RoboRougelike/issues/731) owner-selected host #690, canonical API V3 #729, storage #712/#669 and original #510/#556/#596 remain untouched. **This art-format bridge does NOT fix dual .owner/.lease Save locks**, P0 host security, persistent IDs, session authority or save migration; B01-23/20/62/61/41 own those tasks. No Codex, no merge, no live PC. Original source geometry source-only CI PASS does not prove actual Unity acceptance.

**Receiver B01-30/40** independently verify authenticity, diff and safe disposable scratch import/rollback (including GUID assignment and no foreign empty-directory deletion). **B01-60** use authorized, legally activated local Windows Unity 6000.3.14f1, import generated OBJ using native ModelImporter, verify pivot/scaling/normals/URP material appearance, capture genuine Editor.log/screenshots/test XML and identify if .mtl material remapping needed; if not executed `G1=NOT_RUN`. **B01-10/22** optional original visual HP/damage readability only, not repair authority or economy cadence. B01-00 final source approved handoff only after formal gates.

Six stages `IMPLEMENTED=YES_SOURCE | TESTED=ACTUAL_EXACT_SHA_CI_REQUIRED | QA_ACCEPTED=NO_FORMAL | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO`; source G0 waiting real CI, formal G0 B01-30/40 PENDING, G1 NOT_RUN, G2 HOLD, Codex ACK NO, integrator-feedback NOT_MEASURED.

**Rollback:** remove only five brand-new W23 source/workflow files, or only explicitly W23-named generated OBJ/MTL/PNG/JSON from approved disposable scratch. Preserve original W22 GLB, previous W16-W21 art, foreign even-empty directories, any source under frozen #510/#556/#596, #690/#729/#712 and unrelated agent paths.
