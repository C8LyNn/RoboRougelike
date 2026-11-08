# Catalog wreck repair guidance

Base `ad1170b8`. Selecting a destroyed item keeps the catalog open, but the repair instruction was written only to the obscured garage screen. The existing catalog feedback now receives the same instruction. An existing actual-garage Unity test was extended to check the feedback while the catalog remains open; it also already verifies that a wreck cannot be mounted until repaired.

- Runtime and Editor test-assembly Roslyn compilation with existing Unity references: PASS, exit 0 for each. Logs: `Artifacts/Local/CSharp-CatalogWreckFeedback-20261108/`.
- Extended Unity test and actual GUI/Windows Player: NOT RUN. Confirm visibility in both orientations at the next interactive QA session.
- No build produced.
