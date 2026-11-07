# R4 block source parity check

This lightweight PowerShell check compares the staged `R4BlockCatalog.cs` to the original `R4_BLOCKS_UNITY.json` from #963 commit `03af5207143da76faeeb8004543a51f26c7f9c0d`. It verifies the source SHA-256, all 30 IDs, per-block module permission and contact mask, ordered cells and bonds, and each socket's face, cell, partial/full flag, offset and dimensions. Floating point values allow 0.000051 cell for the source generator's four-decimal rounding.

From the repository root:

```powershell
& .\Tools\R4Blocks\Test-R4BlockCatalogVerifier.ps1 `
  -Source 'PATH_TO_SOURCE\R4_BLOCKS_UNITY.json' `
  -Catalog '.\Assets\R4_1\R4BlockCatalog.cs'
```

The wrapper also runs in-memory negative checks for changed module permission, contact mask, cell and socket size. To validate a deliberately selected newer source revision, call `Verify-R4BlockCatalog.ps1` directly with its exact `-ExpectedSourceSha256`.

To check all 30 source collider files against the exact catalog and source manifest without importing them into Unity:

```powershell
& .\Tools\R4Blocks\Test-R4ColliderSourceVerifier.ps1 `
  -SourceRoot 'PATH_TO_SOURCE\R4Blocks'
```

This checks the pinned catalog and manifest hashes, rejects duplicate or escaping paths, and hashes each referenced collider. Its self-test rejects an intentionally incorrect collider hash. A newer source requires explicitly providing its verified catalog and manifest hashes to `Verify-R4ColliderSources.ps1`.

For a lightweight inspection of the verified GLB containers and their declared mesh/index structure:

```powershell
& .\Tools\R4Blocks\Test-R4ColliderStructure.ps1 `
  -SourceRoot 'PATH_TO_SOURCE\R4Blocks'
```

The structure check validates GLB 2 headers and chunk bounds, embedded buffers, UCX node names, mesh/accessor references and triangle counts. Its self-test rejects deliberately corrupt headers. It reads metadata, not triangle coordinates or mesh topology: it cannot prove convexity, geometry alignment, nonintersection or suitability for Unity MeshCollider.

For a separate lightweight numerical inspection of the source meshes (Node.js, no Unity):

```powershell
node .\Tools\R4Blocks\test-collider-geometry.js
node .\Tools\R4Blocks\verify-collider-geometry.js 'PATH_TO_SOURCE\R4Blocks'
```

The first command tests the checker using a closed convex cube and deliberately open, degenerate, inconsistently wound, invalid-index, dented and malformed examples. The second checks the pinned source hashes and decodes the source's supported embedded position/index accessors. It checks finite coordinates, triangle degeneracy, edge pairing/winding and whether each triangle's plane leaves all vertices on one side. Vertices are welded for edge checks at 1 micrometre; half-space distance tolerance is 0.01 mm. Unsupported formats or node transforms fail closed.

These numerical tests are useful preflight checks, not a proof that Unity's collider cooking, orientation, scale, mounting, contact geometry or game behavior is correct. They do not create or install any collider.

This is a source-data check, not a Unity test or proof of physical geometry, gameplay integration, collision behavior or save compatibility. As of this change the shape catalog and socket policy remain disconnected from the actual garage and saved vehicles.
