# R4 collider bundle validation

`R4ColliderBundle` adds serializable data containers and a fail-closed structural validator for the offline bundle from #1001. It creates no Unity mesh, collider or gameplay behavior. It checks source revision/schema/grid, all 30 catalog IDs, expected piece counts and names, finite flat vertex arrays, triangle indices and degenerate triangles. `IsTrustedSource` independently checks the exact raw UTF-8 bundle SHA-256. A future caller must check the same raw text, immediately deserialize it, then call `Validate`; structural validation alone does not authenticate that its object came from the checked text.

## Lightweight validation

- The actual C# files compiled and ran in the offline single-worker .NET 8 `CatalogSmoke` project with minimal stand-ins for Unity value types. The initial build failed because the validator did not yet exist; after implementation the smoke passed for the real 30-shape/36-piece JSON, the exact raw hash, catalog port quarantine and six invalid object cases (nonfinite position, invalid index, degenerate triangle, duplicate ID, wrong piece name and wrong source revision). Modified raw text was rejected by the hash check.
- The Unity `.meta` GUIDs were created without opening Unity and each appears once in project `Assets`/`Packages` at this revision.
- **NOT RUN:** Unity compilation, `JsonUtility` serialization (a targeted Unity test has been added), Windows Player, collider creation/cooking, orientation, geometry-to-port agreement, garage integration, restoration and gameplay. .NET stubs are not Unity evidence.

The mesh bundle remains under `Tools`, not `Assets` or `Resources`; nothing in the game consumes it. The four source socket discrepancies and the temporary OctColumn module-port quarantine remain unresolved. Source artist files and existing saves are untouched.
