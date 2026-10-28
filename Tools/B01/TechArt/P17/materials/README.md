# B01-30 / PASS17 — MAT-A1-001 R1

New original tileable material texture content for RoboRougelike. **This is NOT a new 3D MODEL_JOB, Unity prefab or approved final art treatment.** The provisional B01-31 A1 wheel+support-arm research direction is the visual reference, not the source of any pixels.

## Actual exported PNG material assets

The generator writes **eight 512 × 512 RGBA PNG textures**: four each for TIRE_RUBBER (blue-graphite dielectric tire chevron-like surface) and BRUSHED_ARM_ALLOY (blue-grey directional brushed metallic support surface). Each profile has basecolor, tangent-space normal, metallic_smoothness and occlusion maps. Includes a rendered swatch contact sheet, actual generated per-file SHA256 manifest, and independent verification receipt. All pixels come from original analytic periodic functions with no external graphic assets.

## Reproduce (standalone checkout, no Unity)

Requires Python 3.10+ and Pillow for preview+QA; source PNG generator itself uses Python standard library.

    python3 Tools/B01/TechArt/P17/materials/generate_materials.py --size 512 --out build/B01-30/MAT-A1-001
    python3 Tools/B01/TechArt/P17/materials/verify_materials.py --input build/B01-30/MAT-A1-001

The own workflow .github/workflows/b01-30-pass17-materials.yml executes both commands and **again** generates all 8 source PNGs into a second output root, compares each binary PNG, generated manifest and rendered sheet. Generated binary textures are downloadable from the workflow artifact; they are not silently presented as already integrated assets.

## URP Lit import and rollback contract

Use **only** in an isolated receiving project when Codex and HQ authorize receiving candidate lookdev. Each family creates a separate URP Lit material. Keep Wrap Mode Repeat, Texture Type and color settings:

| Texture file suffix | URP Lit assignment | Required import |
|---|---|---|
| basecolor | Base Map | sRGB ON, alpha opaque |
| normal | Normal Map | Unity Texture Type Normal map, sRGB OFF, tangent +Y |
| metallic_smoothness | Metallic Map | sRGB OFF, red = metallic, alpha = smoothness; not HDRP MaskMap |
| occlusion | Occlusion Map | sRGB OFF, green = ambient occlusion |

On receiving, choose metallic workflow and smoothness source from metallic map alpha when exposed by the installed URP UI. Inspect with actual mesh UVs and actual URP lighting. This package does not modify actual game Assets, material GUIDs, shaders or cameras. To rollback, delete candidate material references and imported PNGs from an authorized receiving project, restore old materials; delete build/B01-30/MAT-A1-001* staging paths. No main/develop merge.

## True test scope and limits

Independent verification decodes all eight original PNGs, checks dimensions, channels, material nonflatness and tile-edge mean, unit-length tangent normal encodings, SHA256 matches, original-source deterministic re-generation and rejects altered synthetic metallic channels. Separate CI uses fresh-process binary replay. None prove Unity 6000.3.14f1 URP image appearance, correct real object UV mapping/texel density, physical rubber behavior, PhysX, approved palette, or actual in-game material import. All those remain NOT_RUN or HQ_DECISION_PENDING.

**Package:** B01-30-MAT-A1-001 / R1. **Owner:** B01-30. **State:** DRAFT / NOT_OFFERED / NOT_MERGED / NO MODEL_JOB. **Handoff:** B01-40 validates package, B01-60 independently QA, B01-00 orders CODEX INBOX, Codex alone integrates if approved. [Machine contract](package-manifest.json).
