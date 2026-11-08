# Handling, enemy and expedition targeted QA

Scope: incremental work based on `3e1b346f` with the earlier swing-wheel and enemy-boundary fixes. The garage settings panel now shows the live Rigidbody mass, operational wheel count and motor force multiplier; it does not alter vehicle physics or tuning. The current vehicle drive derives its available traction from actual ground contacts and working motors, with mass affecting acceleration and steering.

Unity 6000.3.14f1, EditMode tests entering Play Mode, `Artifacts/Local/RC2-HandlingFlow-Edit-20261108`: process exit 0, 3/3 passed. XML and Editor.log are local artifacts, not committed.

* `SwingWheelsDriveOntoRamp`: actual Rigidbody, R6 wheel contacts, wheel art and existing suspension drove up a 10-degree box ramp. The body rose from y=0.63 to y=1.73 and moved z=8.77 m; this is one controlled ramp, not proof for every obstacle.
* `SixRealEncountersRewardsFinaleAndRestart`: all six encounter transitions, rewards, finale and restart. Enemies are defeated by direct damage in this test; it is not a human or full combat playthrough.
* `SaveAfterVictoryRestoresGarageWithoutDuplicatingPartsOrReward`: post-victory checkpoint and garage restore, without duplicated rewards or parts. It does not simulate a Windows process restart.

Previous focused run `Artifacts/Local/RC2-DriveAndEnemyReview-20261108T100638Z`: actual wheel drive responds to working and damaged motor modules, and starter vehicle differences derive from real parts. Enemy tests and the unresolved narrow-corridor retreat case are recorded in `Docs/ENEMY_BOUNDARY_QA.md`; swing-arm self-clearance and its limits are recorded in `Docs/R4_SWING_CLEARANCE_QA.md`.

Not run: a human full expedition, a broad resolution matrix, all ramp shapes and enemy escape through a corridor too narrow to turn. Do not treat a zero-test run as a pass: `RC2-HandlingFlow-20261108` used the wrong test platform and ran zero cases.
