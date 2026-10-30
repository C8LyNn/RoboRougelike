# B01-30 — PASS 17 / WAVE 08 — ORIGINAL-GLB-BINARY-QA-001/R1

This **executed actual GLB 2.0 binary receiver** for the three real 3D source-locked braced rig previews is tightly scoped to the existing [B01-30 producer Draft #423 @ 60a453e4ecd915725bc2fac5433ba7ad43a628cb](https://github.com/C8LyNn/RoboRougelike/pull/423), as explicitly prioritized for art QA in [WAVE08 #450](https://github.com/C8LyNn/RoboRougelike/issues/450). **No added or altered model, prefab, mesh, part ID, physics, VFX, source JSON, Codex Assets or main/develop merge.** It downloads the original *already generated* 3 GLBs (not a fresh fake source recreation) from [real W06 Actions #41030346462, artifact #12964979343](https://github.com/C8LyNn/RoboRougelike/actions/runs/41030346462/artifacts/12964979343).

## What is actually tested

A **new separate pure-stdlib GLB 2.0 binary parser** with no Blender dependency (independent *implementation* from the producer's earlier Blender importer, but **not an independent B01-40/60 reviewer**) reads real GLB binary headers / 4-byte aligned JSON and BIN chunks; refuses external buffers / required unknown extensions / suspicious/truncated chunks; decodes genuine binary vertex POSITION float32 and indexed triangle accessor data; computes nondegenerate triangle areas on every mesh. It checks world transform node hierarchy, glTF Y-up source cell location (x,y,-z)×0.55m, original localId, source kind, visual-only geometry flags, all 5 authored source Armor brace flags, **PBR material assignment AND actual RGBA** (amber brace vs normal Armor/other kinds), per-file sha256/bytes from original machine receipt. Confirms **43/43 actual mesh-bearing nodes, 5 original brace material assignments and all three GLBs** against **4 unchanged B01-23 source Git blobs** from [#375@5666a39eabd4cea91b2b605a9eb99ea2f1e226e8](https://github.com/C8LyNn/RoboRougelike/pull/375).

**Adversarial negatives:** Mutated original GLB node brace flag/source ID/collider-claim/cell/mesh material/world translation/triangle primitive and required extension, deliberately mutated float32 vertex to NaN in *real BIN chunk*, independently mutated actual original GLB byte to prove strict original SHA gate. Each mutator is tested on **each** real model; mutation semantic tests explicitly bypass *outer hash only*, so they prove semantic receiver rather than trivial SHA. Both OS execute full test and upload machine JSON with actual per-model triangle counts, material counts, original SHA and negative rejection reasons; no fabricated rendered screenshots or Unity proof.

## Reproduction

Use the self-contained [W08 workflow](https://github.com/C8LyNn/RoboRougelike/actions/workflows/b01-30-p17w08-original-glb-binary-gate.yml) (Windows2025 + Ubuntu24.04) or download original W06 Action artifact and original source JSON from exact #375 Git revision, then run:

```bash
python Tools/B01/TechArt/P17W08/P17GlbBinary001/audit_original_glb.py \
  --binary build/B01-30/W08/previous \
  --source build/B01-30/W08/source \
  --out build/B01-30/W08/result
```

The script resolves physical GLBs recursively (exactly one per name), verifies original `source-export-receipt.json`, verifies exact original Git blob hashes and writes `p17-original-glb-binary-qa.json`.

## Limits, ownership, QA, rollback

Source grid observed from original [PARTSIZE #141](https://github.com/C8LyNn/RoboRougelike/issues/141) is not approved collision clearance. These source-locked debug GLBs are original **visual proxies** and their triangles are NOT verified collider hulls or mount sockets, and they do not prove bracing/weakpoint behavior. **Blender author/recipient previous test #423 ≠ native Unity G1.** This new GLB parser also does NOT run actual Unity Editor/PlayMode, Animator, URP or PhysX, and does NOT formally accept #423 independently: only B01-40/60 can publish independent QA.

**Receiver:** B01-40/60 may independently inspect binary tests and cross-OS receipts, then physically re-import the unchanged original artifact into authorized Unity/Blender as appropriate; B01-23 original data owner receives specific discrepancies, if any. **B01-00** alone controls any later Codex queue. **Rollback:** delete only generated `build/B01-30/W08/**` and isolated own QA source files; never modify/remove #423's original objects or original B01-23 files.

**Changed PR scope:** this own `Tools/B01/TechArt/P17W08/P17GlbBinary001/**` plus `.github/workflows/b01-30-p17w08-original-glb-binary-gate.yml` only. Previous [#423](https://github.com/C8LyNn/RoboRougelike/pull/423), [#437](https://github.com/C8LyNn/RoboRougelike/pull/437), [#399](https://github.com/C8LyNn/RoboRougelike/pull/399) stay frozen/unmodified until independent receiver decision.
