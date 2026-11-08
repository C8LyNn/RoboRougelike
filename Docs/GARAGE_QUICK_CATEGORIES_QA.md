# Garage quick categories

Based on Draft PR #1053. The existing garage list is grouped into Bloki,
Koła, Broń and Moduły; switching resets list scroll and leaves actual owned
items, purchase, repair, placement and selected part behavior unchanged.
Landscape and portrait layouts share the same category controls.

- Unity 6000.3.14f1 targeted EditMode runner at
  `0e065ec20748883c1506e9dba9dcecd1c29d7105`: **2/2 PASS**, including
  category coverage and wheel selection/purchase. Evidence:
  `Artifacts/Local/RC2-GarageCategories-20261108T091855Z`.
- Before implementation the new category test failed:
  `Artifacts/Local/RC2-GarageCategoryRed-20261108T091811Z`.
- Windows Player visual layout and manual usability at each supported aspect
  ratio: **NOT RUN** for this exact commit; check with the next combined build.
