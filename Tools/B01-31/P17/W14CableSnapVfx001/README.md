# B01-31 / PASS 17 WAVE 14 — Original Cable Snap 2D VFX

**Package:** `B01-31-CABLE-SNAP-VFX-001 / P17-W14-R1`

One genuinely **new original game-usable visual VFX capability**: three distinct phases depicting **electrical cable severance on an already-detached robot part**, with original visible *two separated cable halves* and procedural impact, unstable electric flash, and cooling smoke. This is a **cosmetic event visual**, not a detach or dock controller and not a game runtime implementation. Inspired by the game's detachable modules but completely independent of all existing gameplay, Save/Load and Codex code.

**Original authored states:** `fracture` – insulation shards, rapidly expanding orange impact ellipse and discontinuity; `discharge` – three animated flickering blue-white forked electric arcs between physically disconnected terminals; `cooldown` – short residual wisps and fading hot ember particles. Each animation consists of **12 genuinely different original 192×192 RGBA frames with 80ms per frame** (960ms phase), no existing stock sprites or third-party graphics. The arcs are an **illustrative effect**, not a real electrical connection or in-game physical permission. The two physical cable halves remain visually disconnected. There is **no green/SUCCESS/SAFE/ALLOWED state**.

## Outputs — real binary artifacts, not just concept prose

The original source generator with pinned Pillow 11.3.0 exports **36 actual 192×192 transparent RGBA PNG sprites**, **three real animated 12×80ms GIF previews**, and **two original 2304×716 RGB dark/light 3×12-frame art storyboards**. `clip_timing.json` provides ordered frame names, 80ms intervals, phases and a **non-looping proposed combined timeline** (`fracture → discharge → cooldown`); there is **no Unity Animator, prefab, scene, script, or playback claim**.

It also exports a real `B0131-CableSnap-UnitySpriteImport.zip` with **77 actual members** inside exclusively `Assets/B0131CableSnapVfx/**`: 36 original PNG Sprite Single imports, 36 stable original `.png.meta` GUID entries, five stable folder `.meta` GUIDs (**41 distinct .meta GUIDs in total**). The ZIP is generated as `ZIP_STORED` with a reproducible timestamp and physically re-opened byte by byte; `receipt.json` pins all actual PNG/GIF/board and inner ZIP SHA256 hashes. These generated Unity import artifacts are **not automatically installed** in the canonical game.

## Reproduce and validate

```bash
python -m pip install "Pillow==11.3.0"
python Tools/B01-31/P17/W14CableSnapVfx001/build_cable_snap.py --output ./out/B01-31-W14
python Tools/B01-31/P17/W14CableSnapVfx001/verify_physical.py --source ./out/B01-31-W14
```

Own GitHub Actions CI compiles both original scripts; actually renders physical output on Ubuntu 24.04, decodes all 36 original PNG, reads three real 12-frame GIF and both boards, checks unique per-phase pixels, alpha and actual detached cable endpoint visibility, real CRC and exact member parity for 77-file ZIP/41 GUIDs. Deliberately rejects six invalid phase/frame cases and corrupted binary PNG/ZIP hashes. Repeats original build and independently verifies **every actual output byte is SHA-identical**. Separate Windows 2022 independent physical receiver downloads the original actual Ubuntu artifact and re-reads all binaries **without invoking the renderer**. This is producer asset QA **not formal B01-40 G0 and not licensed Unity G1**.

## Installation, QA limitations and rollback

**Do not import into canonical Assets/** as part of this PR. Independent B01-40 receiver may inspect physical ZIP/GUID conflicts and rollback. Licensed B01-60 receiver should import **only in authorized disposable Unity 6000.3.14f1 URP** and verify actual SpriteImporter, 2D visibility/alpha/colors, timing and human legibility, logging any genuine native Editor/GPU/PlayMode tests. Until then `NATIVE_G1=NOT_RUN`, `G2=HOLD`, `CODEX_ACK=NO`, `NOT_INTEGRATED`. No event source, class or AnimatorController has been authored; Codex or authorized gameplay owner would have to bind the graphics separately after both source and live gates.

This isolated branch adds only `Tools/B01-31/P17/W14CableSnapVfx001/{build_cable_snap.py,verify_physical.py,README.md,manifest.json}` plus one unique `.github/workflows/b01-31-p17w14-cable-snap-vfx.yml`. **#510 remains frozen**, selected [#510@d94400d](https://github.com/C8LyNn/RoboRougelike/pull/510), #511/#494 alternatives HOLD. [#552](https://github.com/C8LyNn/RoboRougelike/issues/552) Save/Load is the sole responsibility of B01-23/62 [#544/#549](https://github.com/C8LyNn/RoboRougelike/pull/544), negative evidence [#550](https://github.com/C8LyNn/RoboRougelike/pull/550) remains separate; safe rollback #536/#542 belongs to B01-40/62; actual Unity #532 to B01-60. **This visual package changes neither ID persistence nor reattach physics** and never claims any of their gates.

**Rollback:** remove only own five new source/workflow files from an isolated branch. If a separately authorized disposable Unity receiver installs the standalone archive, remove ONLY its new `Assets/B0131CableSnapVfx/` subtree and root `Assets/B0131CableSnapVfx.meta`; **never recursively remove any foreign empty directory, unchanged W06, #510 or other producer/canonical game file**.

**Receivers:** B01-40 physical ZIP/GUID and non-destructive intake G0, B01-60 licensed native Sprite import/URP/player preview G1, B01-12 optional visual UX only after authoritative event design, B01-00 controls any later Codex #79 handoff. See #276 ownership, no merge.
