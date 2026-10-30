# B01-31 — WAVE 09 — WELD-READABILITY-ATLAS-001 / real visual comparison boards

**New usable deliverable:** two actual full-resolution 1396×1826 PNG comparison boards: every source `idle/weld_early/weld_mid/weld_late/cool_start/cool_mid/cool_end` frame displayed at **48/96/192 actual pixels** on **dark and light fixed UI-style backdrops**. Labels, measurable contrast-pixel counts and actual source-immutable frame data are included. This is an independent B01-31 **visual decision asset**, not another B01-32 welding implementation nor the previous B01-31 #454 Dual Recoil ZIP receiver.

## Provenance

The ONLY input is the archived physical original [B01-32 #432](https://github.com/C8LyNn/RoboRougelike/pull/432) at immutable commit `280ed3f59020432132ea5e156c68590d445009c8`, exact original successful [Actions #41030815153](https://github.com/C8LyNn/RoboRougelike/actions/runs/41030815153), [artifact #12966020486](https://github.com/C8LyNn/RoboRougelike/actions/runs/41030815153/artifacts/12966020486). This includes original 7 real CPU-diagnostic PNGs generated from actually compiled C# frames and a genuine 17-path Unity import ZIP with exact pinned inner SHA256 `0d82429073722f3402e25d056502917ec7dcd2fb62256355011457f58711d2ed`. No owner B01-32 source is modified and no previews are invented from instructions. The original C# sample images are **NOT Unity GPU output**.

## Reproduce the real boards (Ubuntu/Windows)

CI first checks original GitHub Actions run SHA/success and artifact name, Git source branch exact SHA, downloads original physical archive and verifies original Unity import ZIP hash and CRC. Then:

```sh
python -m pip install Pillow==11.3.0
python Tools/B01-31/P17/W09WeldSmallScale001/build_boards.py --original original-weld --output output-weld
```

The actual exported `output-weld/WELD_SMALLSCALE_DARK.png`, `output-weld/WELD_SMALLSCALE_LIGHT.png`, and `output-weld/receipt.json` are uploaded together to Actions. The job runs twice on identical original bytes and compares exact receipt SHA256 and each output file hash; Windows and Ubuntu jobs provide independent physical executability evidence. Real checks include 7 source 512px PNG SHA and alpha states, original Unity binary SHA/CRC, exact original input frame ordering, actual 42 resampled/composited pixels sets, 2 physical PNG readbacks and hashes, invisible Idle/End at every scale, multiple in-memory negative controls for false alpha and mutated inputs.

Each case records `pixel_change_ge16` (number of pixels differing from the flat original specified backdrop by at least 16 in an RGB channel), `nonzero_alpha` and `alpha_max`. `low_readability_risk` flags visible frames with fewer than 12 changed pixels; those are **measurement/inspection cues**, not a claim of WCAG or human legibility. 48/96/192 are visual research pixel proposals, not Codex UI scale choices.

## Review, safety, rollback

**B01-32** may inspect specific small-scale contrast warnings and decide later on its OWN original/new source, never overwritten here. **B01-60** needs genuinely licensed Unity 6000.3.14f1 URP scene/GPU/screenshots and human playtesting to validate rendering/contrast; NOT_RUN. **B01-40** confirms the origin and hashes for source-asset intake. **B01-00** controls Codex inbox #79; no Codex ACK or game integration claimed. This visual mock is a flat 2D composition and does not simulate engine lighting, world perspective, sorting, occlusion, tone mapping, post-processing, shader alpha or true physical size.

**Exclusive source ownership:** only `Tools/B01-31/P17/W09WeldSmallScale001/**` plus one W09-specific workflow. Zero `Assets/**`, original `Tools/B01-32/**`, original `#432/#446`, game/Codex/PARTSIZE/CHL changes. No merge. Rollback deletes these new independent files and workflow only from disposable checkout.
