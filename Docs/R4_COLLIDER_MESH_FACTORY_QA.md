# R4 collider mesh factory prototype

`R4ColliderMeshFactory.TryCreate` is an explicit, opt-in helper that accepts the exact pinned bundle text and a shape ID. It verifies the raw hash, deserializes that same text, validates the complete structure, and creates one in-memory `Mesh` per source piece. The caller owns the meshes and must destroy them. Failed creation cleans up every mesh created by that call. Unknown IDs, altered text and invalid data fail closed before a usable result is returned.

No caller is wired into gameplay. The helper creates **no `MeshCollider`**, changes no part, scene, save, placement gate or physics, and does not import the bundle into `Assets`/`Resources`. Source coordinates and triangle winding are preserved without conversion; Unity orientation, scale and cooking are not verified. Each call currently hashes, parses and validates the complete bundle; it must not be used per frame or for mass vehicle creation until caching and performance are designed and measured.

## Lightweight validation

- Actual factory, bundle and catalog C# compiled in the offline single-worker .NET 8 `CatalogSmoke` project using minimal Unity stand-ins. Initial build failed because the factory did not exist. After implementation the smoke passed: three distinct Arch pieces with source data, altered JSON and unknown ID rejected, and a simulated failure during the second mesh released both partially created meshes.
- The two new Unity `.meta` GUIDs were generated without opening Unity and checked for uniqueness in project `Assets`/`Packages`.
- **NOT RUN:** Unity compilation, `JsonUtility`, real `Mesh` construction and destruction, MeshCollider cooking, visual alignment, rotated placement, garage, save/load, Windows Player and gameplay. A native EditMode test has been added for the next Unity test window. The .NET stand-ins cannot establish Unity behavior or native memory/physics safety.

The four source socket discrepancies remain open; the staged OctColumn module-port quarantine is still not connected to the real garage. Existing game behavior remains unchanged.
