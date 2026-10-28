# B01-32 / PASS15 / MUZZLETRAIL-001

Original standalone **research-only** vector animation/source preview, not an importable game feature, not a new CHL/PARTSIZE/gameplay package. 8 authored individual SVG frames under `frames/` + `viewer.html` functional scrub/play/off controls + machine-readable `sequence.json`. Each frame stands alone at 1200x675 and can be rasterized. Art uses only original vector geometry; no external image/font/assets.

**Semantics:** 00 inactive; 01 precharge; 02 ignition; 03 peak; 04 visible trail; 05 decay; 06 residual spark; 07 fully cleared. Timeline times/length/opacity are **unapproved aesthetic proposals**, not gameplay DPS, damage, hitbox, energy or optimization profiling.

**Preview:** Open `viewer.html` in Chromium with sibling `frames/` files. Default idle and VFX OFF suppress all streaks. Play/space cycles; arrow keys and slider scrub; images are real separate SVG source files. XML safety/visual contrast to be verified in real SVG renderer.

**Ownership:** B01-32 isolated research path only. No canonical `Assets/R6` changes and no claims about actual VFX toggle policy, muzzle material implementation, Unity particle emission, or Beam gameplay telegraph. Distinct from B01-32 original-source C# tests already queued CV-06. For an actual Unity implementation, Codex needs a separate approved integration decision; this pack is for lookdev and review only.
