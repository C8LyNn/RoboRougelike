# R4 block integration plan

Source specification: `Tools/ModelFactory/R4Blocks/CODEX_INTEGRATION.md` at #963 `03af5207143da76faeeb8004543a51f26c7f9c0d` and its `integration/R4_BLOCKS_UNITY.json`. Keep existing 0.55 m grid and old saves; no changes to main/develop.

1. Validate all 30 source definitions against BUILD001: unique IDs, occupied cells, declared bonds, socket patch geometry and module-mount policy. Preserve source `mount_modules` and prohibit weapon/mobility mounting on partial ports. A bond declared by source is not yet proof of a physical collider.
2. Introduce a backwards-compatible shape identifier for armor across model, inventory and save/load, with missing legacy values resolving to the existing cube. Apply rotated occupancy and socket definitions through the current BUILD001 installation and restoration gates; maintain one owner/HP/ID per multi-cell part.
3. Attach each approved FBX and verified physical collider geometry to the existing garage, runtime and ghost paths. Reject overlaps and disconnected fragments using the same actual geometry; do not add a parallel mounting or HP system.
4. Expose shapes from the existing part inventory without duplicate purchase flows; ensure the full/partial port rule applies to weapons, wheels and movement devices in all placement, load and restoration paths.
5. Perform targeted native Unity tests for rotations, one/multi-cell footprints, partial/full sockets, weapon/movement rejection, destruction and old-save round trips. Build Windows and test actual garage, combat and reload. Do not claim gameplay integration until these steps are wired and tested.
