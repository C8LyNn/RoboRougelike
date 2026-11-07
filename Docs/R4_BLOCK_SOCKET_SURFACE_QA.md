# R4 block socket surface preflight

Source: #963 commit `03af5207143da76faeeb8004543a51f26c7f9c0d`; source catalog SHA-256 `3b4208e917d7314673f77d16934bb1bf4894d5b7b0839889c5d9a7fbb49adc74`. The checker verifies the staged mesh bundle against that source before inspecting it.

Offline numerical result: **186/190 declared socket patches had all nine sampled points covered; 4/190 had uncovered samples**. This is a sampling result, not proof of complete surface coverage. The source GLBs and catalog were not altered.

| Shape | Socket | Declared kind | Uncovered | Example in metres | Distance to nearest coplanar triangle edge |
|---|---:|---|---:|---|---:|
| R4B_HexColumn | 0, up | partial | 4/9 | (-0.1650, 0.2600, 0.1650) | 0.228 mm |
| R4B_HexColumn | 1, down | partial | 4/9 | (-0.1650, -0.2600, -0.1650) | 0.228 mm |
| R4B_OctColumn | 0, up | full | 4/9 | (-0.2585, 0.2600, 0.2585) | 40.305 mm |
| R4B_OctColumn | 1, down | full | 4/9 | (-0.2585, -0.2600, -0.2585) | 40.305 mm |

The octagonal shape permits modules in the source metadata, so its two full-face declarations are especially important to resolve before allowing guns or mobility devices to rely on them. The hexagonal source already forbids modules; its small discrepancy still affects the declared structural contact patch. These are source-data/geometry discrepancies, not observed gameplay failures: neither shape is integrated into the game yet.

Method: nine points per rectangular patch (corners, edge midpoints and center); 0.55 m grid, 0.52 m cell face, source tangent convention, 0.1 mm coplanarity tolerance and barycentric triangle inclusion. Distance is to the nearest edge of a coplanar source collider triangle. Synthetic square, half-face, opposite-face, oversized, invalid-axis, regular hexagon and chamfered-octagon tests passed; the latter independently reproduce the four uncovered corners. A hole between sampled points may remain undetected.

**Required before release:** source owner/design review of the four declarations and intended collider geometry, then re-export and recheck. Do not silently shrink source patches or relabel full ports without that decision. Unity import, actual collider cooking, rendering, placement, connection graph, save/load and gameplay remain **NOT RUN**.
