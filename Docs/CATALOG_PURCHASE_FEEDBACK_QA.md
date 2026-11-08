# Catalog purchase failure visibility

Base: `87287ffc`. Both portrait and landscape catalog purchase handlers previously wrote a rejected transaction only to the garage message, which is hidden while the catalog is open. They now also write the same message to the existing catalog feedback area. Successful purchases, prices, inventory, and eligibility are unchanged. This makes the failure visible but does not identify its underlying cause.

- Runtime C# Roslyn compilation with existing Unity references: PASS, exit 0. Local log: `Artifacts/Local/CSharp-CatalogPurchaseFeedback-20261108/runtime.log`.
- Actual GUI rendering and interaction in Unity/Windows Player: NOT RUN. At the next isolated GUI session, reproduce a legitimately rejected purchase in both orientations and confirm a visible message; verify successful purchases still select the purchased part and close the catalog. Do not modify player saves.
- No Windows build was produced.
