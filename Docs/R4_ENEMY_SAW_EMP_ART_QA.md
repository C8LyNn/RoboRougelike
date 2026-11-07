# R4 enemy saw and EMP — integration in progress

Follow-up: [native QA on a3d37d30](R4_BACKLOG_NATIVE_QA_20261107.md) supersedes
the interrupted-run status below for targeted EditMode and batch Windows Player.

This branch builds on the rocket/beam integration. Enemy saw and EMP use the
supplied R4 visual models while retaining the original physical saw centre and
EMP pulse origin. If an asset is absent, the legacy geometry remains available.
Saw selection now ignores inactive art left awaiting destruction at frame end.

## Evidence and limitations

- Source at interrupted run: `7c67b4950e201846be85384839c26985feacc7da`.
- The original two native tests failed at `b61695f2` because neither model
  was attached; this was the expected pre-implementation result.
- Unity at `8b90753f`: 2/3 diagnostic cases passed; the salvage regression
  passed after a Unity-null guard. The saw fixture initially failed because it
  placed the target before the kinematic body's transform had been updated.
- Unity at `ec265350`: 2/3 cases passed, including real saw contact and EMP;
  switching from Basic to Saw correctly revealed selection of the inactive
  previous model. The subsequent code change addresses that selection.
- The broader targeted run on `7c67b495` was interrupted at the user's request;
  it produced a partial Editor log, no completed XML and is **NOT RUN** as a
  final result. No tests have yet been executed against the later fallback test.
- A Windows Player API exercise has been written for the saw and EMP but has
  **not** been built or run for this branch. Human visual/gameplay assessment,
  full regression and final release remain **NOT RUN**.
- Known inherited limitations remain: four raw FBX orientation checks failed,
  interactive automated Player shutdown previously crashed for an undetermined
  reason, and the new shaped blocks are not integrated into playable vehicles.

Evidence directories in this isolated checkout are under `Artifacts/Local/`:
`RC2-R4-SawEmp-Diagnostic-20261107T181702Z`,
`RC2-R4-Saw-Switch-Red-20261107T182022Z` and
`RC2-R4-EnemySawEmp-Final-7c67b495-20261107T182148Z`.

## Next verification when the user permits heavier work

Run the targeted native Unity suite and inspect its XML and Editor log on the
exact final code SHA. Then build a unique Windows x64 directory, extract the
ZIP, run the explicit batch Player exercise and verify the saw, EMP, existing
weapons, real contact and process exit. A batch API exercise is not a human
gameplay or visual test. Do not merge into shared branches without authorization.
