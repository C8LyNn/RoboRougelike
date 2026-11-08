# Portrait catalog: item HP and tier

Base: `5a534d24`. The portrait catalog previously displayed the complete item name before HP in a 230-by-28 logical-pixel label. Long names could wrap or clip the useful HP value. The group header already names the part type, so each row now shows the normalized tier and HP first, matching the information emphasized in the landscape catalog. Selection, repair and inventory data are unchanged.

- Runtime C# Roslyn compilation with existing Unity reference assemblies: PASS, exit 0. Local log `Artifacts/Local/CSharp-PortraitCatalogHp-20261108/runtime.log`.
- Actual rendering, clipping, accessibility and interaction in Unity/Windows Player: NOT RUN. During later GUI QA inspect long-named wheel/weapon groups and mixed T1/T2/T3 and damaged items at portrait sizes, then select and repair an item.
- No Windows build produced.
