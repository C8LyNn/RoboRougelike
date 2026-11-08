# Garage inventory repair flow

The landscape inventory previously closed its catalog as soon as an instance was selected, so repair required reopening the catalog. It now offers the existing repair action next to each owned instance. Selecting an intact or partially damaged item still closes the catalog for mounting. Selecting a destroyed item keeps it open, because it cannot be mounted yet; the item remains selected for repair, dismantling or fusion. The portrait catalog uses the same selection behavior and its existing per-item repair controls.

No repair quote, transaction, ownership, fusion, damage or mounting rules were changed. The narrower landscape item and repair controls need visual testing with long names and both small and large windows.

**Verification:** Roslyn parsed the two modified source files and the new targeted test with zero syntax errors; `git diff --check` clean. Unity compilation, EditMode/PlayMode, input and rendered Windows Player are **NOT RUN** in lightweight mode. `UI943PortraitCatalogActionsTests.DestroyedItemKeepsCatalogOpenUntilRepaired` is prepared but not executed: it checks a real inventory instance, condition, cost, identity and selecting it after repair through controller APIs. This test does not click the new landscape row. The existing `5b21` ZIP predates this change.
