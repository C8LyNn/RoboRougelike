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

The first command tests the checker using closed convex examples and deliberately open, degenerate, inconsistently wound, invalid-index, dented, malformed and outside-envelope examples. The second checks the pinned source hashes and decodes the source's supported embedded position/index accessors. It checks finite coordinates, triangle degeneracy, edge pairing/winding and whether each triangle's plane leaves all vertices on one side. It also verifies that each vertex lies within the axis-aligned envelope of the declared cells (0.55 m grid, 0.52 m block, 1 mm tolerance). Vertices are welded for edge checks at 1 micrometre; half-space distance tolerance is 0.01 mm. Unsupported formats or node transforms fail closed.

The envelope is a coarse bounding box. A separate convex-mesh/box separating-axis test checks the interior of unoccupied grid cells inside that box; each empty-cell box is shrunk by 1 mm, and surface contact is not treated as penetration. Synthetic cube and sloped tetrahedron cases cover intersection, separation and boundary contact. The checks permit the gaps between cells of one rigid part and do not check sub-millimetre intrusion, all possible rotated placements or spaces outside the source footprint envelope. These numerical tests are useful preflight checks, not a proof that Unity's collider cooking, orientation, scale, mounting, contact geometry or game behavior is correct. They do not create or install any collider.

## Prepared mesh bundle

`generated/R4BlockColliderMeshes.json` is a deterministic, flat-array export of the 30 source GLBs (36 separate mesh pieces). Each shape retains its ID and source SHA-256; each piece retains its name, positions and triangle indices. Coordinates and winding are copied from the source GLB without an axis conversion; negative zero is normalized to zero because JSON cannot preserve its sign. Native Unity orientation, winding, scale and collider cooking still require verification.

To recheck the committed bundle against the exact source and exercise five corruption cases:

```powershell
node .\Tools\R4Blocks\test-export-colliders.js 'PATH_TO_SOURCE\R4Blocks' '.\Tools\R4Blocks\generated\R4BlockColliderMeshes.json'
```

To create a new bundle, pass a **new** output path; the exporter refuses to overwrite any existing file:

```powershell
node .\Tools\R4Blocks\export-unity-colliders.js 'PATH_TO_SOURCE\R4Blocks' 'NEW_OUTPUT.json'
```

This bundle is under `Tools`, not `Assets` or `Resources`. Nothing in the game reads it yet; it does not add shapes or colliders to a build.

This is a source-data check, not a Unity test or proof of physical geometry, gameplay integration, collision behavior or save compatibility. As of this change the shape catalog and socket policy remain disconnected from the actual garage and saved vehicles.
