# B01-31 — W12 ORIGINAL UNITY ANIMATION CLIP ART ADD-ON

**P17-W12-R1: one concrete new usable capability:** four physically authored **Unity `.anim` AnimationClip files** for the already existing and source-tested [W11 #505](https://github.com/C8LyNn/RoboRougelike/pull/505) original four eight-frame 2D diagnostic animations. W11 exported **32 original RGBA Sprite PNGs and GIF previews**, but no Unity-native `.anim` assets. W12 bridges precisely this missing asset step; **no old W11 or W10 path is edited**.

Each `.anim` is a genuine Unity YAML `AnimationClip` object (Unity serialized class 74) binding `SpriteRenderer.m_Sprite` (class 212) through **nine explicit PPtr references**: original exact-sprite W11 frames at times `0,0.1,...,0.7` seconds and original first frame again at `0.8` seconds. This final closure prevents shortening the intended 800ms cycle to 700ms. `m_AnimationClipSettings.m_LoopTime=1`, `m_SampleRate=10`, explicit `m_StopTime=0.8`, and `m_ClipBindingConstant` references all nine exact source sprite GUIDs. The four filenames correspond to collision overlap, distance limit, unguarded public API and saturated query **DENIED/HOLD** (there is deliberately no SAFE/allowed state).

Physical add-on: `B0131-DockDeniedVfx-UnityClips-ADDON.zip` with **9 own files** under **only** `Assets/B0131DockDeniedVfx/Animations/`: four actual `.anim`, four matching `.anim.meta` and parent folder `.meta`; **5 new unique stable GUIDs** which never replace any of W11's original 38 sprite importer metadata. **No AnimatorController, Animator prefab, game scene, Rigidbody, MonoBehaviour or gameplay code**; Unity clips are asset data usable by a separately authorized host once native QA completes.

## Immutable provenance and actual source test

**Exact original source #505:** `13632b19b8fee3c263aea62bc91535ba45b12168`; original [GitHub Actions #41069320296](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069320296) physically built original W11 import ZIP in [artifact #12982903484](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069320296/artifacts/12982903484). Original inner Unity ZIP known SHA256 `7fc696ef80730192d41fffdb8736f1b6b81ce9611126c4ada7c40a4fc861aaef`. This version consumes original ZIP **read-only** and aborts if ZIP hash or any original 32 actual Sprite PNG bytes/metadata/unique GUID differ. Producer cannot regenerate or modify originals.

Reproduce locally after obtaining and extracting **GitHub Actions artifact #12982903484** to `./w11-original` (containing original `B0131-DockDeniedVfx-UnitySpriteFrames.zip` and `receipt.json`):

```bash
python Tools/B01-31/P17/W12DockClipAssets001/build_clips.py --original ./w11-original --output ./w12-output
python -m pip install "PyYAML==6.0.2"
python Tools/B01-31/P17/W12DockClipAssets001/verify_clips.py --original ./w11-original --addon ./w12-output
```

The original builder writes real `w12-output/loose/Assets/B0131DockDeniedVfx/Animations/*.anim`, matching `.meta`, actual import add-on ZIP and receipt with all original asset sha256/GUID. Executes **seven negative variants**, notably forged fileID refs, absent metadata, bad 700ms loop, broken m_Sprite/class 212 and corrupted source GUID. A *different implementation* on Windows loads physical Unity YAML via the PyYAML parser, checks nine 0.1s keyframes per clip including exact closure to sprite 0 and matches each PPtr GUID to original PNG `.meta` in the physical source ZIP. Dedicated GitHub Actions workflow runs both source generator (Ubuntu) and actual Windows artifact receiver, produces real physical ZIP and source receipt, rerenders twice and checks byte determinism.

## Installation and rollback — permission required

On **authorized disposable Unity 6000.3.14f1 URP** only: first import unchanged, hash-verified W11 **70-member sprite ZIP**; then import W12 **nine-member AnimationClip add-on ZIP**, in that order. Inspect actual `AnimationClip` in Animator/Animation preview with a temporary GameObject carrying `SpriteRenderer`; do not claim Unity G1 until real licensed Unity Editor import, 10fps/eight-frame loop and GPU/alpha visual acceptance are recorded with native Editor.log/PlayMode results. `AnimationClip` does not auto-play: separately authorized Animator/controller binding required. There is **no direct change to canonical Assets/** or any Codex source**, and no scene modifications.

**Safety:** [HQ W12 #518](https://github.com/C8LyNn/RoboRougelike/issues/518) exclusively chooses [B01-23 Secure V2 #510@d94400d](https://github.com/C8LyNn/RoboRougelike/pull/510) for formal G0; [#511](https://github.com/C8LyNn/RoboRougelike/pull/511) and [#494](https://github.com/C8LyNn/RoboRougelike/pull/494) are alternatives and are not installed/combined/QA-certified by these animation assets. Visual effects **cannot guard public Reattach nor solve #486 transactional safety**, do not imply source G0 or Unity G1. B01-40/60 remain independent asset QA receivers; B01-12 for visual UI semantics after authoritative host status; B01-00 solely owns Codex #79 handoff.

**Rollback:** remove only W12 `Tools/B01-31/P17/W12DockClipAssets001/**` and dedicated workflow on disposable branch; if separately imported in disposable Unity, remove only `Assets/B0131DockDeniedVfx/Animations/` and `Animations.meta`, preserving original W11 sprite import folder and all source, scenes, W10 and other producers. Native Unity `NOT_RUN` until verified; G2 HOLD; no Codex OFFERED/ACK/INTEGRATED, no merge.
