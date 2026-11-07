# R4 collider mesh cache prototype

`R4ColliderMeshCache` is an opt-in, instance-owned prototype. Opening it checks and parses the exact trusted bundle once. The first request for a known shape creates its meshes lazily; later requests return the same read-only collection. A failed creation cleans up partial meshes and may be retried. Disposing the cache is idempotent and releases its meshes; requests after disposal fail closed. The original one-shot factory remains available.

The cache creates no `MeshCollider` and has no game caller. The collection wrapper prevents replacing entries, but Unity `Mesh` objects themselves remain mutable: future consumers must not modify or destroy shared meshes and must detach every user before the owning cache is disposed. The cache is not thread-safe; actual Unity `Mesh` creation and disposal must occur on the appropriate Unity thread. Source coordinates and winding remain unchanged.

## Lightweight validation

- Offline single-worker .NET 8 compiled and ran the actual catalog, bundle validator, factory and cache against minimal Unity stand-ins. The pre-implementation build failed due to the missing cache; after implementation checks passed for same-shape reuse, separate shapes, read-only collections, unknown/altered input, partial failure cleanup, retry, disposal and use-after-disposal. Existing catalog, bundle and one-shot factory smoke checks also passed.
- The new Unity `.meta` GUIDs were generated without starting Unity and checked for uniqueness in project `Assets`/`Packages`.
- **NOT RUN:** native Unity compilation and EditMode (a targeted test was added), actual Unity object lifetime, `MeshCollider` cooking, memory/performance profiling, garage, save/load, Windows Player and gameplay. Stand-ins cannot prove native behavior.

The cache does not yet make shapes available to players. Source socket discrepancies and the temporary OctColumn module-port quarantine remain open; the game does not yet consume this catalog or bundle.
