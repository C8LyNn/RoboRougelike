# B01-31 / PASS 17 — WAVE 15 — ORIGINAL CABLE SNAP UNITY ANIMATION CLIPS

**PACKAGE:** `B01-31-CABLE-SNAP-ONE-SHOT-UNITY-CLIPS-001/P17-W15-R1`; exclusively original B01-31 add-on assets, no canonical-game source edits or merge.

## Actual independently useful completion of W14 #557

The prior immutable [original W14 #557 @ `d6eb3406120bef2f921079dffe01d898985d7a25`](https://github.com/C8LyNn/RoboRougelike/pull/557) physically produced 36 real 192px transparent RGBA sprite PNGs and three GIF animations for severance `fracture → discharge → cooldown` with 12×80ms original frames per state. However, its original **77-member Unity Sprite import ZIP contains NO native `.anim` files**; GIF previews and frame timing metadata alone do not let an authorized Unity host play these clips.

This package **fills precisely that missing native Unity asset capability** by authoring **four genuine standalone `AnimationClip` YAML class 74 assets**:
- `B0131_CableSnap_fracture.anim` — 12 `m_Sprite` frames, non-looping 960ms.
- `B0131_CableSnap_discharge.anim` — 12 `m_Sprite` frames, non-looping 960ms.
- `B0131_CableSnap_cooldown.anim` — 12 `m_Sprite` frames, non-looping 960ms.
- `B0131_CableSnap_full_sequence.anim` — one **new uninterrupted 36-frame original one-shot** with complete order `fracture→discharge→cooldown`, duration 2,880ms. This is NOT a new gameplay implementation: it reuses original W14 art frame bytes/GUID unchanged.

All clips bind actual `SpriteRenderer.m_Sprite` Unity classID 212 via PPtr curves (object GUID references) and serialized `m_ClipBindingConstant`. The three phase clips contain **13 keys** each: 12 original sprite frames at `0,0.08,...,0.88`, plus final key at `0.96` **holding the final 12th frame** (prevents a 880ms shortened clip). The full-sequence contains **37 keys**, from `0` through `2.80` plus final hold at `2.88`. All are nonlooping (`m_LoopTime: 0`, `m_SampleRate: 12.5` FPS), `76` source sprite references total (3×13 + 37). **No Sprite PNG was re-rendered, altered or copied into W15 ZIP**.

Actual new independent Unity add-on `B0131-CableSnap-OneShot-UnityClips-ADDON.zip` contains **9 physically authored Unity files ONLY** at `Assets/B0131CableSnapVfx/Animations/**`: four native-format `.anim`, four matching `.anim.meta` and one folder `.meta`, with **five new unique deterministic GUIDs** that do not collide with the existing 36 original sprite GUIDs. The source commits merely include the original authoring/verification code, manifest, README and unique CI workflow; **no repo `Assets/**` change**. In target disposable Unity you must first install the **original unchanged W14 ZIP**, then this additive 9-file ZIP.

## Immutable physical input and repro commands

**Original immutable source**: [original W14 GitHub Actions #41087336619](https://github.com/C8LyNn/RoboRougelike/actions/runs/41087336619), [actual original physical artifact #12991802931](https://github.com/C8LyNn/RoboRougelike/actions/runs/41087336619/artifacts/12991802931). GitHub's OUTER artifact wrapper is distinct from the W14 actual INNER `B0131-CableSnap-UnitySpriteImport.zip`, exact original inner SHA256:

`b164a1a5ac305d1c0147b131eb16f104d64685ea9267b3d8d20340028ba9e902`.

Both original W14 input `receipt.json` and physical 77-member ZIP must be placed together under `./w14-original`. Any byte drift in original W14 ZIP, any of 36 sprite PNG or original GUID `.png.meta` causes **hard fail-closed**. The source does not generate or modify W14 images.

```bash
python -m pip install --disable-pip-version-check PyYAML==6.0.2
python Tools/B01-31/P17/W15CableSnapUnityClips001/build_clips.py --original ./w14-original --output ./w15-out
python Tools/B01-31/P17/W15CableSnapUnityClips001/verify_clips.py --original ./w14-original --addon ./w15-out
```

Original author additionally rejects **eight malformed actual clip asset variants**: looping enabled, shortened 0.88s clip, 10fps timing mismatch, wrong `SpriteRenderer` attribute, forged original GUID reference, shifted 1.04s endpoint, wrong object binding type, missing Unity `.meta`. Actual output includes loose author-created `.anim/.meta`, real nine-member ZIP and actual per-file SHA256 receipt. Physical CI performs **2× complete byte-identical rerenders of all outputs**, then **separate Windows2022 original-independent physical receiver** downloads original Ubuntu output and uses PyYAML to parse real `.anim` `m_PPtrCurves` + all **76 m_Sprite PPtr references** against actual original W14 36 Sprite `.meta` GUIDs; verifier NEVER imports original renderer. Passed source-format tests are not a claim of actual Unity Editor import.

## W15 constraints, formal QA, receiver and rollback

[HQ W15 #571](https://github.com/C8LyNn/RoboRougelike/issues/571) keeps **selected Secure V2 [#510@`d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510)** and **sole durable Save contract [#556@`33094f154c7b91119b36d58aab285e6e83c5de57`](https://github.com/C8LyNn/RoboRougelike/pull/556)** permanently frozen. All other alternatives (#511/#494 and #544/#549/#554) HOLD. **No W15 animation changes Secure Reattach, saves/loads, part ID, Rigidbody parent, rollback, Codex or canonical `Assets/**`.** Source animations are cosmetic only; do not interpret a visual cable state as proof of physics/dock/save transaction status.

**B01-40:** independently validate physical 9-asset ZIP, actual source 36 GUID, imported namespace collision and safe rollback. **B01-60:** if legally activated Unity 6000.3.14f1 URP is available, import both source W14 Sprite ZIP and W15 .anim add-on into an authorized disposable Unity receiver; check actual `SpriteRenderer.m_Sprite`, 12.5fps, 12+hold vs 36+hold, actual one-shot no-loop, and alpha/camera human readability. Capture real `Editor.log`, AssetDatabase errors, PlayMode/Animation window timings; **otherwise NATIVE_G1=NOT_RUN**, no Unity import PASS. B01-12 may propose UI event wiring **only after authoritative gameplay owner signs off**; no automatic Unity AnimatorController/prefab/MonoBehaviour/game event listener included. B01-00 alone may route an authorized future Codex #79 offer; source success does not mean ACK/integration.

**Rollback:** remove ONLY five new W15 tool/workflow paths from isolated branch. On a separately authorized disposable Unity host remove ONLY `Assets/B0131CableSnapVfx/Animations/` and its `Animations.meta`, preserving original W14 `Sprites/`, foreign empty directories, other art assets and Codex. `G0_PRODUCER_SOURCE` pending real CI, `B01-40_FORMAL_G0` PENDING, `UNITY_G1` NOT_RUN, `G2` HOLD, `CODEX_ACK` NO, `INTEGRATED` NO, `NO_MERGE`.
