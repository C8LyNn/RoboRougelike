# B01-31 — DETACHED-IDENTITY-MARKERS-001 / PASS17 WAVE13

**One independent original VISUAL ART functionality.** A set of **16 actual original source-identity-dependent detachable-part marker sprites** for human visual inspection of spatial parts in future debug/playtesting. Each marker has an authored 256×256 RGBA hexagonal salvage silhouette and **5×5 mirrored binary shape** derived from the example opaque ID's SHA256 plus secondary hash-driven notches. The geometric pattern provides identification without depending solely on color; a six-color palette improves scanning but is not the primary identity discriminator. This is original digital art, not a stock asset.

## Why this is needed and what it DOES NOT prove

[HQ W13 #535](https://github.com/C8LyNn/RoboRougelike/issues/535) identified missing detached-part consumed-ID persistence in frozen [selected #510@`d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510). **Actual Save/Load fields, snapshot/restore transaction ordering, stale-ID rejection and migration are exclusively B01-23/B01-62 work** and remain independent of this art.

This tool only consumes **16 illustrative, invented, synthetic case-sensitive strings** in `fixture.json` and a deterministic order permutation called `post_load_simulated`. **No part is ever detached, saved, loaded, or restored by this package.** The fixture is **NOT** a proposed or canonical Save/Load JSON schema and cannot replace actual stable-ID/ledger verification. The generated marker colors/shape can collide for IDs outside the example set, so **never use these markers as security, permission, or actual ID truth**. Pre-save/after-load names on contact boards label synthetic UI fixture comparison ONLY. The current selected gameplay #510 remains frozen; #511/#494 HOLD, no dual install, no safety/rollback claim.

## Real output

The original generator physically creates:
- **16 separate transparent, original 256×256 RGBA PNG marker art sprites**, all uniquely different shapes and byte digests in this fixture.
- **3 physical original 1400×1480 RGB art review PNGs:** `BEFORE_SAVE_SYNTHETIC.png` (dark), `AFTER_LOAD_SIMULATED.png` (dark and deliberately reordered), `AFTER_LOAD_SIMULATED_LIGHT.png` (light theme). Every sample card has the entire exact illustrative ID and an explicit synthetic/not-ledger notice.
- An actual physically importable **`B0131-DetachedIdentityMarkers-UnitySprites.zip` with 34 members**: 16 original PNGs and 16 per-Sprite `.meta`, two folder `.meta`, 18 deterministic original Unity GUIDs. Only the isolated path `Assets/B0131DetachedIdentityMarkers/**` exists INSIDE the output ZIP — never written directly to the repo's canonical `Assets/**`.
- `receipt.json` records actual per-PNG SHA256/byte sizes, 16 per-ID geometric signatures, contrast measurements, all three board hashes, original ZIP SHA256 and each source fixture phase. **No fabricated Unity console output, game Save/Load logs, human playtests or physics.**

## Reproduce the ACTUAL original art

Python 3.11 with pinned Pillow 11.3.0:

```bash
python -m pip install Pillow==11.3.0
python Tools/B01-31/P17/W13DetachedIdentityArt001/build_markers.py \
  --fixture Tools/B01-31/P17/W13DetachedIdentityArt001/fixture.json \
  --output build/B01-31/P17W13/DetachedIdentityMarkers001
python Tools/B01-31/P17/W13DetachedIdentityArt001/verify_artifact.py \
  --fixture Tools/B01-31/P17/W13DetachedIdentityArt001/fixture.json \
  --source build/B01-31/P17W13/DetachedIdentityMarkers001
```

Generator must reject duplicate, missing, renamed/case-changed, foreign, unparseable ID and un-reordered synthetic fixtures; it physically decodes 16 output PNGs, measures 16×2 dark/light pixel contrasts, checks unique 15-bit visual signatures and identity-independent input order, ZIP CRC/34 actual files/18 `.meta` GUIDs. Separate **read-only** physical receiver independently decodes each PNG's **actual 5×3 pixel bits** and compares the geometric signature, checks output SHA, 3 physically rendered boards and original ZIP member byte equality. GitHub CI runs producer twice and separate Windows downloaded-binary receiver; complete raw output artifacts attached to the run. These are *visual data fixture* and art integrity tests — **NOT executed C# snapshot tests**.

## Source owner / blockers / rollback

The W13 branch adds only `Tools/B01-31/P17/W13DetachedIdentityArt001/{build_markers.py,verify_artifact.py,fixture.json,manifest.json,README.md}` and a unique workflow. Prior static art #489, animated VFX #505, new four Unity `.anim` clips #524, thermal vent #520, selected source #510, Codex, B01-23/40/60/61/62 are untouched. No merge. **B01-40** independently checks real ZIP and potential imported GUID/path collisions; **B01-60** licenses Unity 6000.3.14f1 to test actual Sprite import, camera legibility, colors/shape/GPU readability and alpha; **B01-10/12** may decide if future UI diagnostics warrant it. **B01-23/B01-62 alone** own ID persistency and reattachment logic, this package does not validate their Save/Load. **B01-00 alone** may offer #79 after formal gates.

Rollback only own W13 tool/workflow files; future authorized disposable Unity import can remove `Assets/B0131DetachedIdentityMarkers/` and sibling root `.meta`, not any other asset or foreign directory. **Producer SOURCE_ART_G0 only after actual CI, native Unity G1 NOT_RUN; actual Save/Load NOT_RUN; formal independent QA PENDING; G2 HOLD; no Codex ACK or integration.**
