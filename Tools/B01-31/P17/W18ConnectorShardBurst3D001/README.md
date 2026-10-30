# B01-31 — PASS 17 / WAVE 18 — Original Connector Shard Burst 3D

**Package** `B01-31-CONNECTOR-SHARD-BURST3D-001/P17-W18-R1`. Role: standalone original concept/VFX asset. [HQ W18 #632](https://github.com/C8LyNn/RoboRougelike/issues/632), ownership [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). Unlike completed [W17 #614](https://github.com/C8LyNn/RoboRougelike/pull/614) (animated two rigid connector halves), this package delivers **actual 3D fracture debris** on individual newly authored geometry — 16 separately animated original low-poly 3D triangular-prism shards. No previous sprite/frame/model pixels are used or modified.

## Actually usable original 3D visual assets (physical source outputs)

`B0131-ConnectorShardBurst3D.glb`: genuine binary glTF 2.0 with **16 independent 3D prism meshes** and distinct nodes, **128 actual triangles** (8 per closed triangular prism; cap and side surfaces), proper normals/indices, **4 PBR materials** (amber insulation, cyan electric core, graphite, copper), and **32 independent LINEAR motion channels** (16 x world translation and 16 x rotation quaternion), each with **7 actual keyframes = 224 FLOAT32 physical transform samples** in one 1.2-second keyframe animation. Each fragment flies radially outward and remains separated, with individualized axis rotation and physically distinct mesh proportions.

**True animated 3D human previews:** three original `GIF` files, 16 physically rendered frames x 80ms (1280ms GIF time including final hold), for isometric, side, and top cameras. The previews are **rendered by projecting actual animated 3D triangle geometry** — not generated screenshots or stock media. Also **two 2100×520 real dark/light RGB key-pose contact-sheet PNGs** (seven genuine 3D world poses per sheet). Machine-readable `timeline.json`, actual `receipt.json` (SHA256 and bytes per file), and deterministic actual ZIP containing all **8 physical output files**. The generated art has no Unity source prefabs, AnimatorController, collision, gameplay logic or required new C# API; an authorized game engineer may integrate the sprite/GLB only after native QA.

**Scope:** new `Tools/B01-31/P17/W18ConnectorShardBurst3D001/{build_shards.py,verify_shards.py,README.md,manifest.json}` plus one exclusive workflow `.github/workflows/b01-31-p17w18-connector-shard-burst3d.yml`. No canonical `Assets/**` or Codex source edits, frozen selected #510, #556 and #596 entirely unchanged. #604 is **ALT_HOLD/DO NOT INSTALL**, #596 remains sole #B01-23/62 Save/Load adapter source. This 3D visual content has no bearing on attachment permission, persisted IDs, checkpoint generation, Save/Load atomicity or rollback correctness.

## Reproduction / independent physical QA

```bash
python -m pip install --disable-pip-version-check "Pillow==11.3.0"
python Tools/B01-31/P17/W18ConnectorShardBurst3D001/build_shards.py --output build/B0131W18
python Tools/B01-31/P17/W18ConnectorShardBurst3D001/verify_shards.py --source build/B0131W18
```

The real QA must parse the physical GLB JSON+BIN+float32 accessors: **every one of 128 prism faces nondegenerate**, encoded normal and winding consistency, 16 independent geometrically nonidentical meshes, material assignments, all 32 separately targeted tracks, 224 actual motion keys, 7 validated time samples, every shard actually moving outward (no backwards restart), unit quaternions; physically read all three actual 16×80ms animated GIF previews and two RGB storyboard PNGs and verify each original view has uniquely different frames; exact ZIP raw member/loose byte equality and CRC. It rejects 7 intentional wrong GLB header/channel/material/node mutants, regenerates **every real output twice** and compares bytewise SHA for all GLB/GIF/PNG/JSON/ZIP/receipt, then runs a separate **Windows2022 independent physical binary receiver** reading genuine Ubuntu-produced art **without calling the producer**.

Until native Unity 6000.3.14f1 import/URP rendering/PlayMode truly executed by B01-60: **G1=NOT_RUN**. Cross-platform Python GLB/PNG/GIF validation is not Unity import approval; generating original .glb is not proof Unity can import GLB without additional packages. glTF itself does not carry an enforced nonloop flag; host controls playback, despite suggested one-shot. No PhysX test is possible from a GIF/GLB.

**Receiver/rollback:** B01-30/40 independently assess physical glTF asset/source/evidence & safe scratch, B01-60 native licensed model import if available, B01-32 optional approved cosmetic event linkage, B01-00 routes only if gated. Delete only five W18 files to rollback branch; separately authorized disposable importer should delete only `B0131-ConnectorShardBurst3D` scratch asset, never old W17/W16 geometry, occupied/foreign empty folders, #510/#556/#596 or Codex. **No merge / no live installation**.
