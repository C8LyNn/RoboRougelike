# B01-31 / PASS17 WAVE17 — SPLIT COUPLER KINEMATIC glTF 3D ART

**NEW SOURCE VERSION** `B01-31-SPLIT-COUPLER-KINEMATIC-GLB-001/P17-W17-R1` under one exclusive B01-31 producer. Visual-only, entirely isolated. No code in game's canonical `Assets/**`.

## Why it is different from the previous completed assets

Original [W16 PR #599 exact `5df35807c61d3e238a04b3b3a493e095a589b001`](https://github.com/C8LyNn/RoboRougelike/pull/599) already produced **actual real original 3D geometry**: two separated independent pivot roots, **1312 physical triangles**, 6 PBR materials, minimum **0.26m** world-space conductor gap, plus actual OBJ/MTL and two static preview boards. W16 3D GLB contained **NO glTF animation channels**. W17 adds a **genuinely authored temporal 3D behavior** rather than generating a duplicate geometry model or another static storyboard.

W17 uses W16 original physical [run #41097210391](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097210391) and [artifact #12996617273](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097210391/artifacts/12996617273) as **READ-ONLY** input, pinned exact original W16 inner GLB SHA256 `eeae6a553ca6136b5eaa97a65d5dd995f5f28fcb8bc54eb2180dfe28bc3653d7`. The original GLB **vertex, normal, index arrays, all 36 original bufferView and accessor descriptors, both meshes, both roots and all 6 PBR material records remain unchanged**. Original geometry BIN byte prefix must compare **byte-for-byte** with original W16 bytes. No original W16 source edit, no copied third-party media.

New deliverable is **an actually animated binary glTF 2.0 file** `B0131SplitCouplerConcept-SnapRecoil.glb`, one `B0131_SnapRecoil_1p20s` animation of 1.20 seconds, four real LINEAR transform channels — **two distinct node translations and two distinct node local X-axis rotations**. Both separate connector halves recoil outward from x −0.20/+0.20 m to maximum −0.49/+0.49, ending separated at x −0.36/+0.36. Each of the four tracks has **seven authored physical FLOAT32 samples** at `0.0,0.12,0.25,0.40,0.64,0.88,1.20` sec. X-only rotation does NOT modify geometry X bounds; translation never decreases the source 0.26m whole-mesh gap, including between input keys. This is cosmetic robot cable-severing art, **not an actual attach, collision or physics event**.

**One-shot caveat:** glTF animation data has **no engine-native no-loop flag**. Timeline metadata explicitly *recommends* `ONE_SHOT_HOLD` to an eventual authorized host; **DO NOT represent it as Unity-enforced playback, actual Animator code or an integrated behavior**.

Additionally exports **two physically rasterized original 2100×520 contact sheets** (dark/light, each seven actual geometric poses projected from true original W16 triangles), a machine-readable `timeline.json`, a true SHA256 `receipt.json`, and a physical **five-member reproducible original ZIP**. This is 3D animation content, not another 2D VFX or repeated QA-only shim.

## Reproduce original animated 3D art and execute actual physical readback

Extract original W16 [GitHub Actions artifact #12996617273](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097210391/artifacts/12996617273) such that the frozen original **`B0131SplitCouplerConcept.glb`** and **`receipt.json`** are both in `./original-w16/B0131W16/`. The producer **refuses** any wrong source digest, W16 material/mesh count, original 1312 triangles receipt mismatch or missing original physical bytes.

```bash
python -m pip install --disable-pip-version-check "Pillow==11.3.0"
python Tools/B01-31/P17/W17SplitCouplerAnimatedGLB001/build_animated.py \
  --original ./original-w16/B0131W16 --output ./out/B0131W17
python Tools/B01-31/P17/W17SplitCouplerAnimatedGLB001/verify_animated.py \
  --original ./original-w16/B0131W16 --output ./out/B0131W17
```

Physical original CI creates the true glTF2 binary and images; independent verifier decodes real GLB2 JSON/BIN, traces actual four animation FLOAT32 accessors, checks 7 samples/channel and **241 individual interpolated intermediate states** for gap against original actual W16 mesh vertices, two actual separate rotations, 5 original adversarial document mutations + two actual corrupted GLB headers rejected, 7 truly different rendered poses per board, binary ZIP CRC and content parity. The full output (GLB, PNG, JSON, receipt, physical ZIP) is generated **twice**, every physical output SHA identical. Windows2022 independent receiver downloads and reads the **original Ubuntu-produced actual W17 artifact**, not regenerated geometry, alongside **frozen original W16 artifact**. Source-format PASS cannot replace a real licensed Unity native import.

## Owned scope, independent receivers, rollback

Only `Tools/B01-31/P17/W17SplitCouplerAnimatedGLB001/{build_animated.py,verify_animated.py,README.md,manifest.json}` and dedicated new `.github/workflows/b01-31-p17w17-split-coupler-animated-glb.yml` are modified. ZIP outputs live only in GitHub Actions, not in canonical game assets. **No changes to** frozen [#510@`d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510), frozen [#556@`33094f154c7b91119b36d58aab285e6e83c5de57`](https://github.com/C8LyNn/RoboRougelike/pull/556), [W17 adapter contest #596/#604](https://github.com/C8LyNn/RoboRougelike/issues/608), or Codex code. No adapter selection is made by this artist; B01-23/62 alone choose a bridge, B01-61 tests real transactions, B01-40 receives secure migration and B01-60 native test.

**Gates:** B01-31 source art-format G0 only AFTER actual CI; formal independent B01-30/40 intake PENDING, B01-60 activated Unity6000.3.14f1/GPU/actual animation native G1 NOT_RUN, G2 HOLD, CODEX_ACK NO, INTEGRATED NO, NO MERGE. Geometry storyboard/GIF is not a game PlayMode screenshot or real PhysX outcome. **Receivers:** B01-30/40 review original GLB animation/accessors/foreign path/GUID (no new Unity .meta created) and safe scratch import/rollback; B01-60 run true Unity model importer + animation if authorized; B01-12 only optional visual behavior; B01-00 later #79 handoff only after formal gates.

**Rollback:** delete precisely five new W17 own source/workflow paths from isolated branch. If separate authorized disposable Blender/Unity receiver imports the original W17 animated GLB, delete only the isolated W17 `B0131SplitCouplerConcept-SnapRecoil` imported asset and corresponding generated scratch import data, NEVER W16 original, Codex files, foreign empty directories or any frozen source. No merge.
