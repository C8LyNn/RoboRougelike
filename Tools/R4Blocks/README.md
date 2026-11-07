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

This is a source-data check, not a Unity test or proof of physical geometry, gameplay integration, collision behavior or save compatibility. As of this change the shape catalog and socket policy remain disconnected from the actual garage and saved vehicles.
