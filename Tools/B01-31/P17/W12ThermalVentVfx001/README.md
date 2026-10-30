# B01-31 / P17 WAVE12 — THERMAL-VENT-VFX-001

**Real original thermal venting visual effect**, separate from previous B01-31 #505 dock-denial icons and B01-32 #432 weld/scan-sweep asset releases. Two physically authored procedural animation modes illustrate the same robot-part venting mechanic: **hot amber thermal exhaust** and **cyan coolant vapor**. Neither performs any combat, gameplay heat or damage computation.

## Real physical files

The new script `build_vent.py` builds **12 visually distinct transparent RGBA 256×384 sprites per mode**: 24 original frames. Each layer is original, procedurally authored with deterministic parametric curling ribbons, diffusion, soft flare, a four-slit mechanical source and individually generated hot particles versus cold condensation droplets. This is original vector-like generated art, not copied artwork or duplicate W11 warning icons.

The output contains **two actual looping GIFs** (each 12 × 80 ms, 960 ms/loop), **two real 3072×384 horizontal frame strips**, a **3072×836 full 24-frame storyboard** (hot and coolant) and `clip_timing.json` listing every source frame in display order. These are **offline art review previews**, not Unity screen recordings or native shader tests.

The real isolated `B0131-ThermalVent-UnitySpriteImport.zip` has **52 actual physical entries** under `Assets/B0131ThermalVentVfx/`: 24 original RGBA PNG Sprite Single entries, 24 stable Unity Sprite `.png.meta` and 4 folder `.meta`. **28 unique deterministic GUIDs**, no runtime C# files, Unity scene changes, prefab, shader, Animator, existing texture overwrites or model integration. This remains on isolated source PR until B01-40/60 independent QA.

## Exact reproduction

On source checkout, Python 3.11:

```bash
python -m pip install Pillow==11.3.0
python Tools/B01-31/P17/W12ThermalVentVfx001/build_vent.py --output build/B01-31/P17W12/ThermalVent001
python Tools/B01-31/P17/W12ThermalVentVfx001/verify_artifact.py --source build/B01-31/P17W12/ThermalVent001
```

Dedicated GitHub Actions on original source commit builds twice with **full physical PNG/GIF/ZIP/receipt SHA byte comparison** and uploads output. The **read-only receiver is a separately authored Python module**, runs on Windows 2022 using only downloaded Linux-produced real binaries, checks actual frame alpha/corners, full 24 Git file identities, GIF 12×80ms timing, strips and 24-frame storyboard, actual ZIP 52/52 contents/CRC/no-cross-namespace/28 Unity GUIDs and negative tamper controls. Its successful execution is not formal B01-40/60 signoff.

## Safety and integration boundary

[B01 HQ #518](https://github.com/C8LyNn/RoboRougelike/issues/518) selected exactly **Secure Reattach V2 #510 @ `d94400d95d13db7f27e1533df05c9292d5380431`**, with #511/#494 alternative HOLD. This original thermal VFX asset **does not interact with any of them**. No source in `Assets/**`, Codex, B01-23, B01-32 or any earlier B01-31 version is changed. Art is not a heat gameplay implementation, not a claim of safe reattach or a migration.

**B01-40** must independently inspect physical import ZIP hashes, GUIDs, no receiver collision, installer+rollback. **B01-60** must run licensed Unity **6000.3.14f1 URP** Sprite import, actual SpriteRenderer playback and on-camera visibility/human legibility; until then `UNITY_NATIVE_NOT_RUN` and `G2_HOLD`. **B01-32** is optional producer VFX integrator only after that formal QA; **B01-00** sole eventual #79 Codex handoff; Codex ACK never implied. No merge.

**Rollback** own `Tools/B01-31/P17/W12ThermalVentVfx001/**` + one workflow. If an authorized receiver imported in a disposable Unity worktree, remove `Assets/B0131ThermalVentVfx/` plus its folder-level `.meta` only; avoid removing any existing asset or other-agent source.
