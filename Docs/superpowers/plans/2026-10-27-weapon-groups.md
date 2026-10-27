# Weapon Groups R2 Implementation Plan

**Goal:** Equip at most two canonical weapon types and fire each group independently with LMB/RMB.
**Architecture:** Preserve group choices on the existing vehicle model, route triggers through the existing modular assembly, and expose compact garage/combat controls. Do not alter camera or map internals.
**Constraints:** Keep type-limit and per-gun cooldowns, avoid firing on resume, retain previous stacked branches, no merges.

1. Reproduce cross-group firing in native Unity with a real modular vehicle.
2. Add deterministic type assignment, swap and DeepCopy handling; test lost/dismounted types and copies.
3. Route each input group separately, release continuous weapons on pause and gate held input until release; test real shots.
4. Show compact group controls and counts in garage and HUD, retain all reticle markers.
5. Freeze sources, run Unity compile, targeted EditMode/PlayMode and Windows x64; record actual results and publish Draft PR and build.
