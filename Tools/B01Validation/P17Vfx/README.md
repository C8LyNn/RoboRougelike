# B01-40 / PASS17 — Original damage-decal art to importable Unity Sprite Single assets

**PACKAGE_ID:** `B01-40-P17-DECAL-SPRITE-BRIDGE-001` v1.0.0. This is an **integration artifact**, not a new VFX design or canonical game implementation. Original **B01-32 art producer** [#340](https://github.com/C8LyNn/RoboRougelike/pull/340) at immutable Git SHA `829ac8da9db83a49865d24fa05a5143f59d4b83f`, original validated renderer blob `4a2b785fc2a460deea495aa38443d1cdd079cd97`, original asset manifest blob `7f3e2cdbe06300cf4ee641659b6f9b8132a51ad5`. B01-40 owns only `Tools/B01Validation/P17Vfx/**` and one dedicated GitHub Actions workflow.

## What the working tool ACTUALLY builds

- Calls the **unmodified** original B01-32 `render_art.py` on all **12 real SVG art source files** in a newly created detached temporary Git worktree of exact SHA. Calls original alpha/atlas/animation parity assertions. No authored SVG/renderer modifications.
- Independently physically decodes the resulting 12 256×256 transparent RGBA PNGs and 768×1024 RGBA original atlas with Pillow; checks individual **pixel-identical** tiles and clean/opaque state sematics.
- Emits `B0140-PartDamageDecals-UnitySpriteSingles.zip`, a portable fixed-timestamp archive containing **12 original full-resolution PNG files and 12 stable `.png.meta` TextureImporter configurations** (`spriteMode: 1`, `textureType: 8`, alpha transparency, no mipmaps, no sprite physics) in `Assets/B01ImportStaging/B0140DamageDecals/Frames/`, 3 stable folder metas, a reference copy of the original atlas, owner raster receipt and `Reference/codex-sprite-binding.json` with all 12 source state/frame/160 ms/alpha/atlas-coordinate mappings. GUIDs are deterministic from namespaced target paths; the operator verifies no collision against the **actual** immutable Git metadata of base + current pinned Codex source and the B01-32 owner.
- Outputs `B0140-original-source-receipt.json` with SHA256 of ZIP and **all 30 included file bytes**. No Unity Editor was started, so actual import success is **NOT_RUN**.

**Important:** This produces 12 independent **Sprite Single** textures rather than ambiguous 3×4 Sprite Multiple slicing. Each frame has an individual asset GUID, so Codex can select it by `sprite_asset_path` and state/frame in the provided binding manifest, without guessing top-down/bottom-up atlas rows. Original 768×1024 atlas remains included under `Reference/` solely for verification, not copied into game staging.

## Reproduction on Linux (.NET not needed)

Prerequisites Git, Python 3.11+, `CairoSVG==2.7.1`, `Pillow==11.3.0`, no Unity:

```bash
git fetch --no-tags --depth=1 origin 829ac8da9db83a49865d24fa05a5143f59d4b83f
git fetch --no-tags --depth=1 origin be2c4b6ac63e04eff5b046e50641ec8d1da4837f
python -m pip install "CairoSVG==2.7.1" "Pillow==11.3.0"
python Tools/B01Validation/P17Vfx/sprite_import_bridge.py --repo . --out /tmp/B0140-new-p17
B0140_P17_ARTIFACT=/tmp/B0140-new-p17/B0140-PartDamageDecals-UnitySpriteSingles.zip python Tools/B01Validation/P17Vfx/test_sprite_bridge.py -v
python Tools/B01Validation/P17Vfx/sprite_import_bridge.py --repo . --verify-zip /tmp/B0140-new-p17/B0140-PartDamageDecals-UnitySpriteSingles.zip
```

Select an **absent external directory**, not `Assets/`. Original source and render are isolated, and after acceptance the temporary Git worktree is removed. CI builds **twice** independently with the same original source on Ubuntu and uses **the actual produced original ZIP** for read-only independent Windows 2025 pixel/meta checks. Windows does **not** claim independent SVG re-render. Always inspect final workflow and per-OS results before citing PASS.

## Instructions for Codex; rollback and nonconflict boundary

1. B01-00/HQ decides if condition decals are in scope and Codex **explicitly acknowledges exact R1 SHA**. Have independent B01-60 review original binary artifact and source GUID receipt.
2. On a *separate disposable Unity project/branch with a snapshot/backup*, inspect the ZIP and ensure every path is new, and the current Unity `Assets/` does **not** already contain `B01ImportStaging`, same paths or GUID. Don't automatically overwrite.
3. Copy **only** `Assets/B01ImportStaging/... ` PNG + .meta into the isolated Unity project (not `Reference/*`), trigger Unity Editor import, inspect all 12 Sprite assets, alpha/size, color-space, URP visibility, play+stop/hide, Unity-native logs and screenshots. The generated YAML is based on an actual tracked Unity TextureImporter metadata template, but was **not** validated by importing it into Unity 6000.3.14f1; it must not be called native PASS on its own.
4. Only **Codex** binds decals to the actual part-state events, detach/unload and VFX OFF policy. **No HP thresholds, status/mobility economy rule, save format, or gameplay VFX code** is included or authorized by this tool.
5. **Rollback before intake:** delete the external output folder/ZIP. **Rollback after authorized disposable import:** remove only the newly added staging folder and its exact three folder `.meta` files, preserving all pre-existing Canonical Assets and restore any Codex-owned changes from snapshot. Never modify `main`/`develop` without HQ authorization.

**State:** source/render/ZIP/metadata G0 must pass real CI; Unity compile/import, GPU visuals, 720p/1080p readability, game adapter, G1/G2 **NOT_RUN/HOLD** until appropriately authorized.
