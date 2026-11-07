# R4 block catalog staging

Source: #963 commit `03af5207143da76faeeb8004543a51f26c7f9c0d`, `Tools/ModelFactory/R4Blocks/integration/R4BlockCatalog.cs.txt` and `R4_BLOCKS_UNITY.json`. The generated 30-entry sketch was adapted into a compiled catalog. The `mount_modules` flag is preserved for each entry, and `CanMountModule` rejects unknown IDs, invalid socket indices, partial sockets and blocks forbidding modules.

Unity 6000.3.14f1 **2/2 EditMode tests PASS**, exit 0 (`r4-block-catalog-first.xml`). One test checks all 30 entries for unique IDs and validates their declared cell, bond and socket definitions with BUILD001. The other checks full/partial socket and forbidden-module examples. Both tests failed before the catalog existed (`r4-block-catalog-red.xml`). Declared bonds are source metadata; they have **not** been verified against physical colliders.

**NOT YET IN GAME:** the catalog and helper are not connected to the garage, placement/restoration gates, runtime meshes, HP, inventories, save/load or gameplay. Therefore they do not yet enforce the requested rule for player construction and the 30 shapes are not available to players. See `Docs/R4_BLOCK_IMPLEMENTATION_PLAN.md` for remaining steps. Windows Player, physical geometry, legacy saves, full suites and manual play **NOT RUN** for this catalog. No merge to shared branches.
