# Wheel appearance catalog

Production revision: `3f2e3d579bdb56aa5096a60f283f73f29ffcab9c`.
Preview and repeated-mount correction: `8f7b7a6284a053c9f92989609e8de9332f84aa81`.
Based on `fix/rocket-reload-hud` (including the previous camera, weapon,
mounting, garage and block-cube work).

The garage now exposes the existing medium side-mounted off-road and slick
wheel appearances as separate catalog stacks. Purchasing selects the requested
appearance directly; the owned item retains its appearance through checkpoint
round trips, and fusion still requires the same appearance. Both appearances
deliberately use the existing collider and identical driving properties. The
other source models are not represented as separate physical wheel types.

Unity 6000.3.14f1 native EditMode: **13 passed, 0 failed, 0 skipped**;
process exit code 0. Filter: `R4WheelVariantsTests`,
`UI943PortraitCatalogActionsTests`, `WheelMountOrientationTests`,
`BacklogPartFusionTests`. Local XML, process data, and Editor log:
`Artifacts/Local/RC2-WheelCatalog-20261107T234800Z/`.

Windows Player visual checks for this revision: **NOT RUN**. The tests execute
the real catalog actions and checkpoint code; they are not a substitute for a
manual evaluation of the interface or for implementing distinct wheel physics.

The first real-mount regression fixture found no clear mounting location on
the unmodified starter vehicle (9 passed, 1 failed). After adding an actual
armor support, the unchanged production correction was rerun at
`e81a879fa2f16958baacb892700ea3f0458bd708`: **10 passed, 0 failed, 0 skipped**,
native exit code 0. It exercises switching the actual ghost and mounting a
slick wheel, followed by selection from the same owned stack. Evidence:
`Artifacts/Local/RC2-WheelCatalogGhost-20261107T235121Z/` and
`Artifacts/Local/RC2-WheelGhostRetest-20261107T235340Z/`.
